using AutoEncodeServer.Data;
using AutoEncodeServer.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace AutoEncodeServer.EncodingJob
{
    public partial class EncodingJobManager : IEncodingJobManager
    {
        private void Build(IEncodingJobModel job, string ffmpegDir, string hdr10plusExtractorPath, string dolbyVisionExtractorPath,
                    string x265Path, bool dolbyVisionEnabled, CancellationToken cancellationToken)
        {
            job.SetStatus(EncodingJobStatus.BUILDING);
            job.SetBuildingStatus(EncodingJobBuildingStatus.BUILDING);

            // Verify source file is still here
            if (File.Exists(job.SourceFullPath) is false)
            {
                job.SetError(Logger.LogError($"Source file no longer found for {job}"));
                return;
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // STEP 1: Initial ffprobe
                try
                {
                    job.SetBuildingStatus(EncodingJobBuildingStatus.PROBING);
                    ProbeData probeData = GetProbeData(job.SourceFullPath, ffmpegDir);

                    if (probeData is not null)
                    {
                        job.SetSourceStreamData(probeData.ToSourceStreamData());
                    }
                    else
                    {
                        // Set error and end
                        job.SetError(Logger.LogError($"Failed to get probe data for {job.FileName}"));
                        return;
                    }
                }
                catch (Exception ex)
                {
                    job.SetError(Logger.LogException(ex, $"Error getting probe or source file data for {job}", details: new { job.SourceFullPath, ffmpegDir }));
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();

                // STEP 2: Get ScanType
                try
                {
                    job.SetBuildingStatus(EncodingJobBuildingStatus.SCAN_TYPE);
                    VideoScanType scanType = GetVideoScan(job.SourceFullPath, ffmpegDir, cancellationToken);

                    if (scanType.Equals(VideoScanType.UNDETERMINED))
                    {
                        job.SetError(Logger.LogError($"Failed to determine VideoScanType for {job}."));
                        return;
                    }
                    else
                    {
                        job.SetSourceScanType(scanType);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    job.SetError(Logger.LogException(ex, $"Error determining VideoScanType for {job}", details: new { job.SourceFullPath, ffmpegDir }));
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();

                // STEP 3: Determine Crop
                try
                {
                    job.SetBuildingStatus(EncodingJobBuildingStatus.CROP);
                    string crop = GetCrop(job.SourceFullPath, ffmpegDir, cancellationToken);

                    if (string.IsNullOrWhiteSpace(crop))
                    {
                        job.SetError(Logger.LogError($"Failed to determine crop for {job}"));
                    }
                    else
                    {
                        job.SetSourceCrop(crop);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    job.SetError(Logger.LogException(ex, $"Error determining crop for {job}", details: new { job.SourceFullPath, ffmpegDir }));
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();

                // OPTIONAL STEP: Create HDR metadata file if needed
                try
                {
                    if (job.SourceStreamData.VideoStream.HasDynamicHDR is true)
                    {
                        job.SetBuildingStatus(EncodingJobBuildingStatus.DYNAMIC_HDR);
                        HDRData hdrData = job.SourceStreamData.VideoStream.HDRData;
                        if (hdrData.HDRFlags.HasFlag(HDRFlags.HDR10PLUS))
                        {
                            // If we aren't given a path, skip this step;  It will be treated as HDR10
                            if (string.IsNullOrWhiteSpace(hdr10plusExtractorPath) is false)
                            {
                                string filePath = CreateHDRMetadataFile(job.SourceFullPath, HDRFlags.HDR10PLUS, ffmpegDir, hdr10plusExtractorPath, cancellationToken);
                                if (string.IsNullOrWhiteSpace(filePath) is false)
                                {
                                    job.AddSourceHDRMetadataFilePath(HDRFlags.HDR10PLUS, filePath);
                                }

                            }
                            else
                            {
                                Logger.LogWarning($"No HDR10+ Metadata Extractor given for {job.Name}. Will not use HDR10+.");
                            }
                        }

                        if (hdrData.HDRFlags.HasFlag(HDRFlags.DOLBY_VISION) && dolbyVisionEnabled is true)
                        {
                            // If we aren't given a path, skip this step;  It will be treated as HDR10
                            if (!string.IsNullOrWhiteSpace(dolbyVisionExtractorPath))
                            {
                                string filePath = CreateHDRMetadataFile(job.SourceFullPath, HDRFlags.DOLBY_VISION, ffmpegDir, dolbyVisionExtractorPath, cancellationToken);
                                if (string.IsNullOrWhiteSpace(filePath) is false)
                                {
                                    job.AddSourceHDRMetadataFilePath(HDRFlags.DOLBY_VISION, filePath);
                                }
                            }
                            else
                            {
                                Logger.LogWarning($"No DolbyVision Metadata Extractor given for {job.Name}. Will not use DolbyVision.");
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    job.SetError(Logger.LogException(ex, $"Error creating HDR metadata file for {job}",
                        details: new { job.Id, job.Name, DynamicHDR = job.SourceStreamData.VideoStream.HasDynamicHDR, dolbyVisionEnabled, dolbyVisionExtractorPath, hdr10plusExtractorPath }));
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();

                // STEP 4: Decide Encoding Options
                try
                {
                    job.SetBuildingStatus(EncodingJobBuildingStatus.INSTRUCTIONS);
                    DetermineEncodingInstructions(job);

                    if (job.EncodingInstructions is null) throw new Exception("EncodingInstructions null.");
                }
                catch (Exception ex)
                {
                    job.SetError(Logger.LogException(ex, $"Error building encoding instructions for {job}", details: new { job.Id, job.Name }));
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();

                // STEP 5: Create FFMPEG command
                try
                {
                    job.SetBuildingStatus(EncodingJobBuildingStatus.COMMAND);
                    if (job.EncodingInstructions.VideoStreamEncodingInstructions.HasDolbyVision)
                    {
                        BuildDolbyVisionEncodingCommandArguments(job, ffmpegDir, x265Path);

                        if (job.EncodingCommandArguments is DolbyVisionEncodingCommandArguments args)
                        {
                            if (string.IsNullOrWhiteSpace(args.VideoEncodingCommandArguments) ||
                                string.IsNullOrWhiteSpace(args.AudioSubsEncodingCommandArguments) ||
                                string.IsNullOrWhiteSpace(args.MergeCommandArguments))
                            {
                                throw new Exception("Empty dolby vision encoding command argument string returned.");
                            }
                        }
                        else
                        {
                            throw new Exception("Null or invalid dolby vision encoding command arguments");
                        }
                    }
                    else
                    {
                        BuildFFmpegCommandArguments(job);

                        if (job.EncodingCommandArguments is EncodingCommandArguments args)
                        {
                            if (string.IsNullOrWhiteSpace(args.FFmpegEncodingCommandArguments))
                            {
                                throw new Exception("Empty encoding command argument string returned.");
                            }
                        }
                        else
                        {
                            throw new Exception("Null or invalid encoding command arguments");
                        }
                    }
                }
                catch (Exception ex)
                {
                    job.SetError(Logger.LogException(ex, $"Error building FFmpeg command for {job}", details: new { job.Id, job.Name }));
                    return;
                }

            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning($"Build was cancelled for {job} - Build Step: {job.BuildingStatus.GetDescription()}");
                return;
            }
            catch (Exception ex)
            {
                job.SetError(Logger.LogException(ex, $"Error building encoding job for {job}", details: new { job.Id, job.Name, job.BuildingStatus }));
                return;
            }

            job.SetStatus(EncodingJobStatus.BUILT);
            job.SetBuildingStatus(EncodingJobBuildingStatus.BUILT);
            Logger.LogInfo($"Successfully built {job} encoding job.");
        }

        #region BuildEncodingJob Private Functions
        /// <summary> Gets <see cref="ProbeData"/> from given source file. </summary>
        /// <param name="sourceFullPath">Full path of source file.</param>
        /// <param name="ffmpegDir">Directory FFmpeg is located in.</param>
        /// <returns><see cref="ProbeData"/></returns>
        private static ProbeData GetProbeData(string sourceFullPath, string ffmpegDir)
        {
            string ffprobeArgs = $"-v quiet -read_intervals \"%+#2\" -print_format json -show_format -show_streams -show_entries frame \"{sourceFullPath}\"";

            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = Path.Combine(ffmpegDir, "ffprobe"),
                Arguments = ffprobeArgs,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            StringBuilder sbFfprobeOutput = new();

            using (Process ffprobeProcess = new())
            {
                ffprobeProcess.StartInfo = startInfo;
                ffprobeProcess.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null) sbFfprobeOutput.AppendLine(e.Data);
                };
                ffprobeProcess.Start();
                ffprobeProcess.BeginOutputReadLine();
                ffprobeProcess.WaitForExit();
            }

            return JsonConvert.DeserializeObject<ProbeData>(sbFfprobeOutput.ToString());
        }

        /// <summary> Gets crop of source file from ffmpeg. </summary>
        /// <param name="sourceFullPath">Full path of source file.</param>
        /// <param name="ffmpegDir">Directory FFmpeg is located in</param>
        /// <returns>String of crop in this format: "XXXX:YYYY:AA:BB"</returns>
        private static string GetCrop(string sourceFullPath, string ffmpegDir, CancellationToken cancellationToken)
        {
            string ffmpegArgs = $"-i \"{sourceFullPath}\" -vf cropdetect -f null -";
            Process cropProcess = null;
            CancellationTokenRegistration tokenRegistration = cancellationToken.Register(() =>
            {
                cropProcess?.Kill(true);
            });

            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = Path.Combine(ffmpegDir, "ffmpeg"),
                Arguments = ffmpegArgs,
                UseShellExecute = false,
                RedirectStandardError = true
            };

            string latestCropLine = string.Empty;

            using (cropProcess = new())
            {
                cropProcess.StartInfo = startInfo;
                cropProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (string.IsNullOrWhiteSpace(e.Data) is false && e.Data.Contains("crop=")) latestCropLine = e.Data;
                };
                cropProcess.Start();
                cropProcess.BeginErrorReadLine();
                cropProcess.WaitForExit();
            }

            tokenRegistration.Unregister();

            cancellationToken.ThrowIfCancellationRequested();

            return latestCropLine.TrimEnd(Environment.NewLine.ToCharArray())[latestCropLine.IndexOf("crop=")..].Remove(0, 5);
        }

        /// <summary> Gets the <see cref="VideoScanType"/> of the file. </summary>
        /// <param name="sourceFullPath">Full path of source file.</param>
        /// <param name="ffmpegDir">Directory FFmpeg is located in</param>
        /// <returns>The <see cref="VideoScanType"/> of the file.</returns>
        private static VideoScanType GetVideoScan(string sourceFullPath, string ffmpegDir, CancellationToken cancellationToken)
        {
            string ffmpegArgs = $"-filter:v idet -frames:v 10000 -an -f rawvideo -y {Lookups.NullLocation} -i \"{sourceFullPath}\"";
            Process scanTypeProcess = null;
            CancellationTokenRegistration tokenRegistration = cancellationToken.Register(() =>
            {
                scanTypeProcess?.Kill(true);
            });

            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = Path.Combine(ffmpegDir, "ffmpeg"),
                Arguments = ffmpegArgs,
                UseShellExecute = false,
                RedirectStandardError = true
            };

            StringBuilder sbScan = new();

            using (scanTypeProcess = new())
            {
                scanTypeProcess.StartInfo = startInfo;
                scanTypeProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (string.IsNullOrWhiteSpace(e.Data) is false && e.Data.Contains("frame detection")) sbScan.AppendLine(e.Data);
                };

                scanTypeProcess.Start();
                scanTypeProcess.BeginErrorReadLine();
                scanTypeProcess.WaitForExit();
            }

            tokenRegistration.Unregister();

            cancellationToken.ThrowIfCancellationRequested();

            IEnumerable<string> frameDetections = sbScan.ToString().TrimEnd(Environment.NewLine.ToCharArray()).Split(Environment.NewLine);

            List<(int tff, int bff, int prog, int undet)> scan = new();
            foreach (string frame in frameDetections)
            {
                MatchCollection matches = Regex.Matches(frame.Remove(0, 34), @"\d+");
                scan.Add(new(Convert.ToInt32(matches[0].Value), Convert.ToInt32(matches[1].Value), Convert.ToInt32(matches[2].Value), Convert.ToInt32(matches[3].Value)));
            }

            int[] frame_totals = new int[4];

            foreach ((int tff, int bff, int prog, int undet) in scan)
            {
                // Should always be the order of: TFF, BFF, PROG
                frame_totals[(int)VideoScanType.INTERLACED_TFF] += tff;
                frame_totals[(int)VideoScanType.INTERLACED_BFF] += bff;
                frame_totals[(int)VideoScanType.PROGRESSIVE] += prog;
                frame_totals[(int)VideoScanType.UNDETERMINED] += undet;
            }

            return (VideoScanType)Array.IndexOf(frame_totals, frame_totals.Max());
        }

        /// <summary>Creates the Dynamic HDR Metadata file (.json or .bin) for ffmpeg to ingest when encoding</summary>
        /// <param name="sourceFullPath">Full path of source file.</param>
        /// <param name="hdrFlag"><see cref="HDRFlags"/></param>
        /// <param name="ffmpegDir"></param>
        /// <param name="extractorFullPath">Full path of dynamic hdr data extractor to use.</param>
        /// <returns>Full path of created metadata file.</returns>
        /// <exception cref="Exception">Thrown if metadata file not created.</exception>
        private static string CreateHDRMetadataFile(string sourceFullPath, HDRFlags hdrFlag, string ffmpegDir, string extractorFullPath, CancellationToken cancellationToken)
        {
            string metadataOutputFile = $"{Path.GetTempPath()}{Path.GetFileNameWithoutExtension(sourceFullPath).Replace('\'', ' ')}{(hdrFlag.Equals(HDRFlags.HDR10PLUS) ? ".json" : ".RPU.bin")}";
            Process hdrMetadataProcess = null;
            CancellationTokenRegistration tokenRegistration = cancellationToken.Register(() =>
            {
                hdrMetadataProcess?.Kill(true);
            });

            string ffmpegArgs;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string extractorArgs = hdrFlag.Equals(HDRFlags.HDR10PLUS) ? $"'{extractorFullPath}' extract -o '{metadataOutputFile}' - " :
                                                                        $"'{extractorFullPath}' extract-rpu - -o '{metadataOutputFile}'";

                ffmpegArgs = $"-c \"{Path.Combine(ffmpegDir, "ffmpeg")} -nostdin -i '{sourceFullPath.Replace("'", "'\\''")}' -c:v copy -vbsf hevc_mp4toannexb -f hevc - | {extractorArgs}\"";
            }
            else
            {
                string extractorArgs = hdrFlag.Equals(HDRFlags.HDR10PLUS) ? $"\"{extractorFullPath}\" extract -o \"{metadataOutputFile}\" - " :
                                                                        $"\"{extractorFullPath}\" extract-rpu - -o \"{metadataOutputFile}\"";

                ffmpegArgs = $"/C \"\"{Path.Combine(ffmpegDir, "ffmpeg")}\" -i \"{sourceFullPath}\" -c:v copy -vbsf hevc_mp4toannexb -f hevc - | {extractorArgs}\"";
            }

            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/bin/bash" : "cmd",
                Arguments = ffmpegArgs,
                UseShellExecute = false
            };

            using (hdrMetadataProcess = new())
            {
                hdrMetadataProcess.StartInfo = startInfo;
                hdrMetadataProcess.Start();
                hdrMetadataProcess.WaitForExit();
            }

            tokenRegistration.Unregister();

            cancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(metadataOutputFile))
            {
                FileInfo metadataFileInfo = new(metadataOutputFile);

                if (metadataFileInfo.Length > 0)
                {
                    return metadataOutputFile;
                }
                else
                {
                    throw new Exception("HDR Metadata file was created but is empty.");
                }
            }
            else
            {
                throw new Exception("HDR Metadata file was not created/does not exist.");
            }
        }

        /// <summary> Determines/Builds <see cref="EncodingInstructions"/> for the given stream data. </summary>
        /// <param name="streamData"><see cref="SourceStreamData"/></param>
        /// <returns><see cref="EncodingInstructions"/></returns>
        private static void DetermineEncodingInstructions(IEncodingJobModel job)
        {
            ISourceStreamData streamData = job.SourceStreamData;
            EncodingInstructions instructions = new();

            VideoStreamEncodingInstructions videoStreamEncodingInstructions = new()
            {
                VideoEncoder = streamData.VideoStream.ResoultionInt >= Lookups.MinX265ResolutionInt ? VideoEncoder.LIBX265 : VideoEncoder.LIBX264,
                BFrames = 8,
                Deinterlace = !streamData.VideoStream.ScanType.Equals(VideoScanType.PROGRESSIVE),
                Crop = true
            };

            if (streamData.VideoStream.HasDynamicHDR)
            {
                HDRData hdrData = streamData.VideoStream.HDRData;
                videoStreamEncodingInstructions.HDRFlags |= HDRFlags.HDR10;
                videoStreamEncodingInstructions.DynamicHDRMetadataFullPaths = new Dictionary<HDRFlags, string>();
                // Go through each possible metadata entry
                foreach (KeyValuePair<HDRFlags, string> path in hdrData.DynamicMetadataFullPaths)
                {
                    // If added, shouldn't be null/empty but double check anyways
                    if (!string.IsNullOrWhiteSpace(path.Value))
                    {
                        // Set flag AND add path
                        videoStreamEncodingInstructions.HDRFlags |= path.Key;
                        videoStreamEncodingInstructions.DynamicHDRMetadataFullPaths.Add(path.Key, path.Value);

                        if (path.Key.Equals(HDRFlags.DOLBY_VISION))
                        {
                            instructions.EncodedVideoFullPath = job.DestinationFullPath.Replace(Path.GetExtension(job.DestinationFullPath), ".hevc").Replace('\'', ' ');
                            instructions.EncodedAudioSubsFullPath = job.DestinationFullPath.Replace(Path.GetExtension(job.DestinationFullPath), ".as.mkv").Replace('\'', ' ');
                        }

                    }
                }
            }
            else if (streamData.VideoStream.HasHDR)
            {
                videoStreamEncodingInstructions.HDRFlags = HDRFlags.HDR10;
            }
            else
            {
                videoStreamEncodingInstructions.HDRFlags = HDRFlags.NONE;
            }

            videoStreamEncodingInstructions.PixelFormat = videoStreamEncodingInstructions.VideoEncoder.Equals(VideoEncoder.LIBX265) ? "yuv420p10le" : "yuv420p";
            videoStreamEncodingInstructions.CRF = videoStreamEncodingInstructions.VideoEncoder.Equals(VideoEncoder.LIBX265) ? 20 : 16;
            instructions.VideoStreamEncodingInstructions = videoStreamEncodingInstructions;

            List<AudioStreamEncodingInstructions> audioInstructions = new();

            IEnumerable<IGrouping<string, AudioStreamData>> streamsByLanguage = streamData.AudioStreams.GroupBy(x => x.Language);
            foreach (IGrouping<string, AudioStreamData> audioData in streamsByLanguage)
            {
                AudioStreamData bestQualityAudioStream = audioData.Where(x => x.Commentary is false).MaxBy(x => Lookups.AudioCodecPriority.IndexOf(x.CodecName.ToLower()));
                IEnumerable<AudioStreamData> commentaryAudioStreams = audioData.Where(x => x.Commentary is true);

                if (bestQualityAudioStream.CodecName.Equals("ac3", StringComparison.OrdinalIgnoreCase) && bestQualityAudioStream.Channels < 2)
                {
                    // If ac3 and mono, go ahead and convert to AAC
                    audioInstructions.Add(new()
                    {
                        SourceIndex = bestQualityAudioStream.AudioIndex,
                        AudioCodec = AudioCodec.AAC,
                        Language = bestQualityAudioStream.Language,
                        Title = bestQualityAudioStream.Title
                    });
                }
                else
                {
                    audioInstructions.Add(new()
                    {
                        SourceIndex = bestQualityAudioStream.AudioIndex,
                        AudioCodec = AudioCodec.COPY,
                        Language = bestQualityAudioStream.Language,
                        Title = bestQualityAudioStream.Title
                    });

                    audioInstructions.Add(new()
                    {
                        SourceIndex = bestQualityAudioStream.AudioIndex,
                        AudioCodec = AudioCodec.AAC,
                        Language = bestQualityAudioStream.Language,
                        Title = bestQualityAudioStream.Title
                    });
                }

                foreach (AudioStreamData commentaryStream in commentaryAudioStreams)
                {
                    // Just copy all commentary streams
                    audioInstructions.Add(new()
                    {
                        SourceIndex = commentaryStream.AudioIndex,
                        AudioCodec = AudioCodec.COPY,
                        Language = commentaryStream.Language,
                        Title = commentaryStream.Title,
                        Commentary = true
                    });
                }
            }

            instructions.AudioStreamEncodingInstructions = audioInstructions.OrderBy(x => x.Commentary) // Put commentaries at the end
                .ThenBy(x => x.Language.Equals(Lookups.PrimaryLanguage, StringComparison.OrdinalIgnoreCase)) // Put non-primary languages first
                .ThenBy(x => x.Language) // Not sure if needed? Make sure languages are together
                .ThenByDescending(x => x.AudioCodec.Equals(AudioCodec.COPY)) // Put COPY before anything else
                .ToList();

            List<SubtitleStreamEncodingInstructions> subtitleInstructions = new();
            if (streamData.SubtitleStreams?.Any() ?? false)
            {
                foreach (SubtitleStreamData stream in streamData.SubtitleStreams)
                {
                    subtitleInstructions.Add(new()
                    {
                        SourceIndex = stream.SubtitleIndex,
                        Forced = stream.Forced,
                        Title = stream.Title
                    });
                }
            }

            instructions.SubtitleStreamEncodingInstructions = subtitleInstructions.OrderBy(x => x.Forced).ToList();

            job.SetEncodingInstructions(instructions);
        }

        /// <summary>Builds and sets command arguments for the given encoding job</summary>
        /// <param name="job">The encoding job (<see cref="IEncodingJobModel"/>)</param>
        /// <exception cref="Exception">Something went wrong/invalid instructions.</exception>
        /// <exception cref="NotImplementedException">Potentially unimplemented instructions.</exception>
        private static void BuildFFmpegCommandArguments(IEncodingJobModel job)
        {
            ISourceStreamData streamData = job.SourceStreamData;
            EncodingInstructions instructions = job.EncodingInstructions;

            VideoStreamEncodingInstructions videoInstructions = instructions.VideoStreamEncodingInstructions;

            // Format should hopefully always add space to end of append
            const string format = "{0} ";
            StringBuilder sbArguments = new();
            sbArguments.AppendFormat(format, $"-y -nostdin -i \"{job.SourceFullPath}\"");

            // Map Section
            sbArguments.AppendFormat(format, "-map 0:v:0");
            foreach (AudioStreamEncodingInstructions audioInstructions in instructions.AudioStreamEncodingInstructions)
            {
                sbArguments.AppendFormat(format, $"-map 0:a:{audioInstructions.SourceIndex}");
            }
            foreach (SubtitleStreamEncodingInstructions subtitleInstructions in instructions.SubtitleStreamEncodingInstructions)
            {
                sbArguments.AppendFormat(format, $"-map 0:s:{subtitleInstructions.SourceIndex}");
            }

            // Video Section
            string deinterlace = videoInstructions.Deinterlace is true ? $"yadif=1:{(int)streamData.VideoStream.ScanType}:0" : string.Empty;
            string crop = videoInstructions.Crop is true ? $"crop={streamData.VideoStream.Crop}" : string.Empty;
            string videoFilter = string.Empty;

            if (!string.IsNullOrWhiteSpace(deinterlace) || !string.IsNullOrWhiteSpace(crop))
            {
                videoFilter = $"-vf \"{HelperMethods.JoinFilter(", ", crop, deinterlace)}\"";
            }

            sbArguments.AppendFormat(format, $"-pix_fmt {videoInstructions.PixelFormat}");
            if (videoInstructions.VideoEncoder.Equals(VideoEncoder.LIBX265))
            {
                HDRData hdr = streamData.VideoStream.HDRData;
                sbArguments.AppendFormat(format, "-c:v libx265").AppendFormat(format, "-preset slow").AppendFormat(format, $"-crf {videoInstructions.CRF}");
                if (!string.IsNullOrWhiteSpace(videoFilter)) sbArguments.AppendFormat(format, videoFilter);
                sbArguments.Append($"-x265-params \"bframes={videoInstructions.BFrames}:keyint=120:repeat-headers=1:")
                    .Append($"{(string.IsNullOrWhiteSpace(streamData.VideoStream.ColorPrimaries) ? string.Empty : $"colorprim={streamData.VideoStream.ColorPrimaries}:")}")
                    .Append($"{(string.IsNullOrWhiteSpace(streamData.VideoStream.ColorTransfer) ? string.Empty : $"transfer={streamData.VideoStream.ColorTransfer}:")}")
                    .Append($"{(string.IsNullOrWhiteSpace(streamData.VideoStream.ColorSpace) ? string.Empty : $"colormatrix={streamData.VideoStream.ColorSpace}:")}")
                    .Append($"{(streamData.VideoStream.ChromaLocation is null ? string.Empty : $"chromaloc={(int)streamData.VideoStream.ChromaLocation}")}");

                if (videoInstructions.HasHDR)
                {
                    // HDR10 data; Always add
                    sbArguments.Append($":master-display='G({hdr.Green_X},{hdr.Green_Y})B({hdr.Blue_X},{hdr.Blue_Y})R({hdr.Red_X},{hdr.Red_Y})WP({hdr.WhitePoint_X},{hdr.WhitePoint_Y})L({hdr.MaxLuminance},{hdr.MinLuminance})':max-cll={streamData.VideoStream.HDRData.MaxCLL}");

                    if (videoInstructions.HDRFlags.HasFlag(HDRFlags.HDR10PLUS))
                    {
                        videoInstructions.DynamicHDRMetadataFullPaths.TryGetValue(HDRFlags.HDR10PLUS, out string metadataPath);
                        if (!string.IsNullOrWhiteSpace(metadataPath))
                        {
                            sbArguments.Append($":dhdr10-info='{metadataPath}'");
                        }
                    }
                    if (videoInstructions.HDRFlags.HasFlag(HDRFlags.DOLBY_VISION))
                    {
                        throw new NotSupportedException($"Cannot build encoding arguments for DolbyVision with this method: {nameof(BuildFFmpegCommandArguments)}.");
                    }
                }
                sbArguments.AppendFormat(format, '"');
            }
            else if (videoInstructions.VideoEncoder.Equals(VideoEncoder.LIBX264))
            {
                sbArguments.AppendFormat(format, "-c:v libx264").AppendFormat(format, "-preset veryslow");
                if (!string.IsNullOrWhiteSpace(videoFilter)) sbArguments.AppendFormat(format, videoFilter);
                sbArguments.AppendFormat(format, $"-x264-params \"bframes=16:b-adapt=2:b-pyramid=normal:partitions=all\" -crf {videoInstructions.CRF}");
            }
            else
            {
                throw new NotImplementedException("Unknown VideoEncoder. Unable to build ffmpeg arguments.");
            }

            // Audio Section
            for (int i = 0; i < instructions.AudioStreamEncodingInstructions.Count; i++)
            {
                AudioStreamEncodingInstructions audioInstruction = instructions.AudioStreamEncodingInstructions[i];
                if (audioInstruction.AudioCodec.Equals(AudioCodec.UNKNOWN))
                {
                    throw new Exception("AudioCodec not set (Unknown). Unable to build ffmpeg arguments");
                }
                else if (audioInstruction.AudioCodec.Equals(AudioCodec.COPY))
                {
                    if (audioInstruction.Commentary is true)
                    {
                        sbArguments.AppendFormat(format, $"-c:a:{i} copy -disposition:a:{i} comment");
                    }
                    else
                    {
                        sbArguments.AppendFormat(format, $"-c:a:{i} copy");
                    }
                }
                else
                {
                    sbArguments.AppendFormat(format, $"-c:a:{i} {audioInstruction.AudioCodec.GetDescription()}")
                        .AppendFormat(format, $"-ac:a:{i} 2 -b:a:{i} 192k -filter:a:{i} \"aresample=matrix_encoding=dplii\"")
                        .AppendFormat(format, $"-metadata:s:a:{i} title=\"Stereo ({audioInstruction.AudioCodec.GetDescription()})\"")
                        .AppendFormat(format, $"-metadata:s:a:{i} language=\"{audioInstruction.Language}\"");
                }
            }

            // Subtitle Section
            for (int i = 0; i < instructions.SubtitleStreamEncodingInstructions.Count; i++)
            {
                SubtitleStreamEncodingInstructions subtitleInstruction = instructions.SubtitleStreamEncodingInstructions[i];
                if (subtitleInstruction.Forced is true)
                {
                    sbArguments.AppendFormat(format, $"-c:s:{i} copy -disposition:s:{i} forced");
                }
                else
                {
                    sbArguments.AppendFormat(format, $"-c:s:{i} copy");
                }
            }

            sbArguments.Append($"-max_muxing_queue_size 9999 -metadata title=\"{job.Name}\" \"{job.DestinationFullPath}\"");

            job.SetEncodingCommandArguments(new EncodingCommandArguments()
            {
                FFmpegEncodingCommandArguments = sbArguments.ToString()
            });
        }

        private static void BuildDolbyVisionEncodingCommandArguments(IEncodingJobModel job, string ffmpegDirectory, string x265FullPath)
        {
            const string format = "{0} ";
            string encodedVideoFullPath = job.EncodingInstructions.EncodedVideoFullPath;
            ISourceStreamData streamData = job.SourceStreamData;

            string videoEncodingCommandArguments;
            string audioSubEncodingCommandArguments;
            string mergeCommandArguments;

            // Video extraction/encoding
            StringBuilder sbVideo = new();
            string ffmpegFormatted;
            string sourceFormatted;
            string x265Formatted;
            string outputFormatted;
            string dolbyVisionPathFormatted;
            string masterDisplayFormatted;
            string maxCLLFormatted;

            VideoStreamEncodingInstructions videoInstructions = job.EncodingInstructions.VideoStreamEncodingInstructions;
            HDRData hdr = streamData.VideoStream.HDRData;
            videoInstructions.DynamicHDRMetadataFullPaths.TryGetValue(HDRFlags.DOLBY_VISION, out string dolbyVisionMetadataPath);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ffmpegFormatted = $"'{Path.Combine(ffmpegDirectory, "ffmpeg")}'";
                sourceFormatted = $"'{job.SourceFullPath.Replace("'", "'\\''")}'";
                x265Formatted = $"'{x265FullPath}'";
                outputFormatted = $"'{encodedVideoFullPath}'";
                masterDisplayFormatted = $"'G({hdr.Green_X},{hdr.Green_Y})B({hdr.Blue_X},{hdr.Blue_Y})R({hdr.Red_X},{hdr.Red_Y})WP({hdr.WhitePoint_X},{hdr.WhitePoint_Y})L({hdr.MaxLuminance},{hdr.MinLuminance})'";
                maxCLLFormatted = $"'{streamData.VideoStream.HDRData.MaxCLL}'";
                dolbyVisionPathFormatted = $"'{dolbyVisionMetadataPath}'";
            }
            else
            {
                ffmpegFormatted = $"\"{Path.Combine(ffmpegDirectory, "ffmpeg")}\"";
                sourceFormatted = $"\"{job.SourceFullPath}\"";
                x265Formatted = $"\"{x265FullPath}\"";
                outputFormatted = $"\"{encodedVideoFullPath}\"";
                masterDisplayFormatted = $"\"G({hdr.Green_X},{hdr.Green_Y})B({hdr.Blue_X},{hdr.Blue_Y})R({hdr.Red_X},{hdr.Red_Y})WP({hdr.WhitePoint_X},{hdr.WhitePoint_Y})L({hdr.MaxLuminance},{hdr.MinLuminance})\"";
                maxCLLFormatted = $"\"{streamData.VideoStream.HDRData.MaxCLL}\"";
                dolbyVisionPathFormatted = $"\"{dolbyVisionMetadataPath}\"";
            }

            sbVideo.AppendFormat(format, $"{(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "-c" : "/C")}")
                .AppendFormat(format, $"\"{ffmpegFormatted} -y -hide_banner -loglevel error -nostdin -i {sourceFormatted}");

            if (videoInstructions.Crop is true) sbVideo.AppendFormat(format, $"-vf crop={streamData.VideoStream.Crop}");

            sbVideo.AppendFormat(format, $"-an -sn -f yuv4mpegpipe -strict -1 -pix_fmt {videoInstructions.PixelFormat} - |")
                .AppendFormat(format, $"{x265Formatted} - --input-depth 10 --output-depth 10 --y4m --preset slow --crf {videoInstructions.CRF} --bframes {videoInstructions.BFrames}")
                .AppendFormat(format, $"--repeat-headers --keyint 120")
                .AppendFormat(format, $"--master-display {masterDisplayFormatted}")
                .AppendFormat(format, $"--max-cll {maxCLLFormatted} --colormatrix {streamData.VideoStream.ColorSpace} --colorprim {streamData.VideoStream.ColorPrimaries} --transfer {streamData.VideoStream.ColorTransfer}")
                .AppendFormat(format, $"--dolby-vision-rpu {dolbyVisionPathFormatted} --dolby-vision-profile 8.1 --vbv-bufsize 120000 --vbv-maxrate 120000");

            if (videoInstructions.HDRFlags.HasFlag(HDRFlags.HDR10PLUS))
            {
                videoInstructions.DynamicHDRMetadataFullPaths.TryGetValue(HDRFlags.HDR10PLUS, out string hdr10PlusMetadataPath);
                if (!string.IsNullOrWhiteSpace(hdr10PlusMetadataPath))
                {
                    sbVideo.AppendFormat(format, $"--dhdr10-info '{hdr10PlusMetadataPath}'");
                }
            }

            sbVideo.Append($"{outputFormatted}\"");
            videoEncodingCommandArguments = sbVideo.ToString();

            // Audio/Sub extraction/encoding
            StringBuilder sbAudioSubs = new();
            sbAudioSubs.AppendFormat(format, $"-y -nostdin -i \"{job.SourceFullPath}\" -vn");
            foreach (AudioStreamEncodingInstructions audioInstructions in job.EncodingInstructions.AudioStreamEncodingInstructions)
            {
                sbAudioSubs.AppendFormat(format, $"-map 0:a:{audioInstructions.SourceIndex}");
            }
            foreach (SubtitleStreamEncodingInstructions subtitleInstructions in job.EncodingInstructions.SubtitleStreamEncodingInstructions)
            {
                sbAudioSubs.AppendFormat(format, $"-map 0:s:{subtitleInstructions.SourceIndex}");
            }

            for (int i = 0; i < job.EncodingInstructions.AudioStreamEncodingInstructions.Count; i++)
            {
                AudioStreamEncodingInstructions audioInstruction = job.EncodingInstructions.AudioStreamEncodingInstructions[i];
                if (audioInstruction.AudioCodec.Equals(AudioCodec.UNKNOWN))
                {
                    throw new Exception("AudioCodec not set (Unknown). Unable to build ffmpeg arguments");
                }
                else if (audioInstruction.AudioCodec.Equals(AudioCodec.COPY))
                {
                    if (audioInstruction.Commentary is true)
                    {
                        sbAudioSubs.AppendFormat(format, $"-c:a:{i} copy -disposition:a:{i} comment");
                    }
                    else
                    {
                        sbAudioSubs.AppendFormat(format, $"-c:a:{i} copy");
                    }
                }
                else
                {
                    sbAudioSubs.AppendFormat(format, $"-c:a:{i} {audioInstruction.AudioCodec.GetDescription()}")
                        .AppendFormat(format, $"-ac:a:{i} 2 -b:a:{i} 192k -filter:a:{i} \"aresample=matrix_encoding=dplii\"")
                        .AppendFormat(format, $"-metadata:s:a:{i} title=\"Stereo ({audioInstruction.AudioCodec.GetDescription()})\"")
                        .AppendFormat(format, $"-metadata:s:a:{i} language=\"{audioInstruction.Language}\"");
                }
            }

            for (int i = 0; i < job.EncodingInstructions.SubtitleStreamEncodingInstructions.Count; i++)
            {
                SubtitleStreamEncodingInstructions subtitleInstruction = job.EncodingInstructions.SubtitleStreamEncodingInstructions[i];
                if (subtitleInstruction.Forced is true)
                {
                    sbAudioSubs.AppendFormat(format, $"-c:s:{i} copy -disposition:s:{i} forced");
                }
                else
                {
                    sbAudioSubs.AppendFormat(format, $"-c:s:{i} copy");
                }
            }

            sbAudioSubs.AppendFormat(format, $"-max_muxing_queue_size 9999 \"{job.EncodingInstructions.EncodedAudioSubsFullPath}\"");
            audioSubEncodingCommandArguments = sbAudioSubs.ToString();

            // Merging
            StringBuilder sbMerge = new();
            sbMerge.AppendFormat(format, $"-o \"{job.DestinationFullPath}\" --compression -1:none \"{encodedVideoFullPath}\" --compression -1:none \"{job.EncodingInstructions.EncodedAudioSubsFullPath}\"")
                .Append($"--title \"{job.Name}\"");
            mergeCommandArguments = sbMerge.ToString();

            job.SetEncodingCommandArguments(new DolbyVisionEncodingCommandArguments()
            {
                VideoEncodingCommandArguments = videoEncodingCommandArguments,
                AudioSubsEncodingCommandArguments = audioSubEncodingCommandArguments,
                MergeCommandArguments = mergeCommandArguments
            });
        }
        #endregion BuildEncodingJob Private Functions
    }
}
