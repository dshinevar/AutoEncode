using AutoEncodeServer.Data;
using AutoEncodeServer.Models.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Base;
using AutoEncodeUtilities.Communication;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace AutoEncodeServer.Models;

// BUILD
public partial class EncodingJobModel :
    ModelBase,
    IEncodingJobModel
{
    [GeneratedRegex(@"\d+")]
    private static partial Regex VideoScanRegex();

    public void Build(CancellationTokenSource cancellationTokenSource)
    {
        const string loggerThreadName = $"{nameof(EncodingJobModel)}_Build";

        TaskCancellationTokenSource = cancellationTokenSource;
        Status = EncodingJobStatus.BUILDING;
        BuildingStatus = EncodingJobBuildingStatus.BUILDING;

        CancellationToken cancellationToken = cancellationTokenSource.Token;

        if (File.Exists(SourceFullPath) is false)
        {
            SetError(Logger.LogError($"Source file no longer found for {this}", loggerThreadName, new { SourceFullPath }));
            return;
        }

        HelperMethods.DebugLog($"BUILD STARTED: {this}", nameof(EncodingJobModel));

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // STEP 1: Initial ffprobe
            try
            {
                BuildingStatus = EncodingJobBuildingStatus.PROBING;
                ProbeData probeData = GetProbeData();

                if (probeData is not null)
                {
                    SourceStreamData = probeData.ToSourceStreamData();
                    Title = probeData.GetTitle();
                }
                else
                {
                    // Set error and end
                    SetError(Logger.LogError($"Failed to get probe data for {Filename}", loggerThreadName, new { SourceFullPath }));
                    return;
                }
            }
            catch (Exception ex)
            {
                SetError(Logger.LogException(ex, $"Error getting probe or source file data for {this}", loggerThreadName, new { SourceFullPath, State.FFmpegDirectory }), ex);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // STEP 2: Get ScanType
            try
            {
                BuildingStatus = EncodingJobBuildingStatus.SCAN_TYPE;
                VideoScanType scanType = GetVideoScan(cancellationToken);

                if (scanType.Equals(VideoScanType.UNDETERMINED))
                {
                    SetError(Logger.LogError($"Failed to determine VideoScanType for {this}.", loggerThreadName, new { SourceFullPath, State.FFmpegDirectory }));
                    return;
                }
                else
                {
                    SetSourceScanType(scanType);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SetError(Logger.LogException(ex, $"Error determining VideoScanType for {this}", loggerThreadName, new { SourceFullPath, State.FFmpegDirectory }), ex);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // STEP 3: Determine Crop
            try
            {
                BuildingStatus = EncodingJobBuildingStatus.CROP;
                string crop = GetCrop(cancellationToken);

                if (string.IsNullOrWhiteSpace(crop))
                {
                    SetError(Logger.LogError($"Failed to determine crop for {this}", loggerThreadName, new { SourceFullPath, State.FFmpegDirectory }));
                }
                else
                {
                    SetSourceCrop(crop);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SetError(Logger.LogException(ex, $"Error determining crop for {this}", loggerThreadName, new { SourceFullPath, State.FFmpegDirectory }), ex);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // OPTIONAL STEP: Create HDR metadata file if needed
            try
            {
                if (SourceStreamData.VideoStream.HasDynamicHDR is true)
                {
                    BuildingStatus = EncodingJobBuildingStatus.DYNAMIC_HDR;
                    HDRData hdrData = SourceStreamData.VideoStream.HDRData;
                    if (hdrData.HDRFlags.HasFlag(HDRFlags.HDR10PLUS))
                    {
                        // If we aren't given a path, skip this step;  It will be treated as HDR10
                        if (string.IsNullOrWhiteSpace(State.HDR10PlusExtractorFullPath) is false)
                        {
                            string filePath = CreateHDRMetadataFile(HDRFlags.HDR10PLUS, cancellationToken);
                            if (string.IsNullOrWhiteSpace(filePath) is false)
                            {
                                AddSourceHDRMetadataFilePath(HDRFlags.HDR10PLUS, filePath);
                            }
                        }
                        else
                        {
                            Logger.LogWarning($"No HDR10+ Metadata Extractor given. Will not use HDR10+ for {Name}.", loggerThreadName);
                        }
                    }

                    if (hdrData.HDRFlags.HasFlag(HDRFlags.DOLBY_VISION) && State.DolbyVisionEncodingEnabled is true)
                    {
                        // If we aren't given a path, skip this step;  It will be treated as HDR10
                        if (!string.IsNullOrWhiteSpace(State.DolbyVisionExtractorFullPath))
                        {
                            string filePath = CreateHDRMetadataFile(HDRFlags.DOLBY_VISION, cancellationToken);
                            if (string.IsNullOrWhiteSpace(filePath) is false)
                            {
                                AddSourceHDRMetadataFilePath(HDRFlags.DOLBY_VISION, filePath);
                            }
                        }
                        else
                        {
                            Logger.LogWarning($"No DolbyVision Metadata Extractor given. Will not use DolbyVision for {Name}.", loggerThreadName);
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
                SetError(Logger.LogException(ex, $"Error creating HDR metadata file for {this}", loggerThreadName,
                    new { Id, Name, DynamicHDR = SourceStreamData.VideoStream.HasDynamicHDR, State.DolbyVisionEncodingEnabled, State.DolbyVisionExtractorFullPath, State.HDR10PlusExtractorFullPath }), ex);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // STEP 4: Decide Encoding Options
            try
            {
                BuildingStatus = EncodingJobBuildingStatus.INSTRUCTIONS;
                DetermineEncodingInstructions();

                if (EncodingInstructions is null)
                    throw new Exception("EncodingInstructions null.");
            }
            catch (Exception ex)
            {
                SetError(Logger.LogException(ex, $"Error building encoding instructions for {this}", loggerThreadName, new { Id, Name }), ex);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // STEP 5: Create FFMPEG command
            try
            {
                BuildingStatus = EncodingJobBuildingStatus.COMMAND;
                if (EncodingInstructions.VideoStreamEncodingInstructions.HasDolbyVision)
                {
                    BuildDolbyVisionEncodingCommandArguments();

                    if (EncodingCommandArguments.IsDolbyVision is true)
                    {
                        if (EncodingCommandArguments.CommandArguments.Any(string.IsNullOrWhiteSpace))
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
                    BuildFFmpegCommandArguments();

                    if (EncodingCommandArguments.IsDolbyVision is false)
                    {
                        if (string.IsNullOrWhiteSpace(EncodingCommandArguments.CommandArguments[0]))
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
                SetError(Logger.LogException(ex, $"Error building FFmpeg command for {this}", loggerThreadName, new { Id, Name }), ex);
                return;
            }

        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning($"Build was cancelled for {this} - Build Step: {BuildingStatus.GetDescription()}", loggerThreadName);
            return;
        }
        catch (Exception ex)
        {
            SetError(Logger.LogException(ex, $"Error building encoding job for {this}", loggerThreadName, new { Id, Name, BuildingStatus }), ex);
            return;
        }

        Status = EncodingJobStatus.BUILT;
        BuildingStatus = EncodingJobBuildingStatus.BUILT;
        Logger.LogInfo($"Successfully built {this} encoding job.");

        #region Local Functions
        ProbeData GetProbeData()
        {
            string ffprobeArgs = $"-v quiet -read_intervals \"%+#2\" -print_format json -show_format -show_streams -show_entries frame \"{SourceFullPath}\"";

            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = Path.Combine(State.FFmpegDirectory, "ffprobe"),
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

            string stringProbeOutput = sbFfprobeOutput.ToString().Trim();

            if (stringProbeOutput.IsValidJson() is true)
            {
                return JsonSerializer.Deserialize<ProbeData>(stringProbeOutput, CommunicationConstants.SerializerOptions);
            }

            return null;
        }

        // Returns string of crop in this format: "XXXX:YYYY:AA:BB"
        string GetCrop(CancellationToken cancellationToken)
        {
            string ffmpegArgs = $"-i \"{SourceFullPath}\" -vf cropdetect -f null -";
            Process cropProcess = null;
            CancellationTokenRegistration tokenRegistration = cancellationToken.Register(() =>
            {
                cropProcess?.Kill(true);
            });

            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = Path.Combine(State.FFmpegDirectory, "ffmpeg"),
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

        VideoScanType GetVideoScan(CancellationToken cancellationToken)
        {
            string ffmpegArgs = $"-filter:v idet -frames:v 10000 -an -f rawvideo -y {Lookups.NullLocation} -i \"{SourceFullPath}\"";
            Process scanTypeProcess = null;
            CancellationTokenRegistration tokenRegistration = cancellationToken.Register(() =>
            {
                scanTypeProcess?.Kill(true);
            });

            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = Path.Combine(State.FFmpegDirectory, "ffmpeg"),
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

            List<(int tff, int bff, int prog, int undet)> scan = [];
            foreach (string frame in frameDetections)
            {
                MatchCollection matches = VideoScanRegex().Matches(frame.Remove(0, 34));
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

        // Creates the Dynamic HDR Metadata file (.json or .bin) for ffmpeg to ingest when encoding
        string CreateHDRMetadataFile(HDRFlags hdrFlag, CancellationToken cancellationToken)
        {
            string metadataOutputFile = $"{Path.GetTempPath()}{Path.GetFileNameWithoutExtension(SourceFullPath).Replace('\'', ' ')}{(hdrFlag.Equals(HDRFlags.HDR10PLUS) ? ".json" : ".RPU.bin")}";
            Process hdrMetadataProcess = null;
            CancellationTokenRegistration tokenRegistration = cancellationToken.Register(() =>
            {
                hdrMetadataProcess?.Kill(true);
            });

            string ffmpegArgs;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string extractorArgs = hdrFlag.Equals(HDRFlags.HDR10PLUS) ? $"'{State.HDR10PlusExtractorFullPath}' extract -o '{metadataOutputFile}' - " :
                                                                            $"'{State.DolbyVisionExtractorFullPath}' extract-rpu - -o '{metadataOutputFile}'";

                ffmpegArgs = $"-c \"{Path.Combine(State.FFmpegDirectory, "ffmpeg")} -nostdin -i '{SourceFullPath.Replace("'", "'\\''")}' -c:v copy -bsf:v hevc_mp4toannexb -f hevc - | {extractorArgs}\"";
            }
            else
            {
                string extractorArgs = hdrFlag.Equals(HDRFlags.HDR10PLUS) ? $"\"{State.HDR10PlusExtractorFullPath}\" extract -o \"{metadataOutputFile}\" - " :
                                                                            $"\"{State.DolbyVisionExtractorFullPath}\" extract-rpu - -o \"{metadataOutputFile}\"";

                ffmpegArgs = $"/C \"\"{Path.Combine(State.FFmpegDirectory, "ffmpeg")}\" -i \"{SourceFullPath}\" -c:v copy -bsf:v hevc_mp4toannexb -f hevc - | {extractorArgs}\"";
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

        void DetermineEncodingInstructions()
        {
            SourceStreamData streamData = SourceStreamData;
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
                            instructions.EncodedVideoFullPath = DestinationFullPath.Replace(Path.GetExtension(DestinationFullPath), ".hevc").Replace('\'', ' ');
                            instructions.EncodedAudioSubsFullPath = DestinationFullPath.Replace(Path.GetExtension(DestinationFullPath), ".as.mkv").Replace('\'', ' ');
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

            List<AudioStreamEncodingInstructions> audioInstructions = [];

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

            instructions.AudioStreamEncodingInstructions = [.. audioInstructions.OrderBy(x => x.Commentary) // Put commentaries at the end
            .ThenBy(x => x.Language.Equals(Lookups.PrimaryLanguage, StringComparison.OrdinalIgnoreCase)) // Put non-primary languages first
            .ThenBy(x => x.Language) // Not sure if needed? Make sure languages are together
            .ThenByDescending(x => x.AudioCodec.Equals(AudioCodec.COPY))];

            List<SubtitleStreamEncodingInstructions> subtitleInstructions = [];
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

            EncodingInstructions = instructions;
        }

        void BuildFFmpegCommandArguments()
        {
            SourceStreamData streamData = SourceStreamData;
            EncodingInstructions instructions = EncodingInstructions;

            VideoStreamEncodingInstructions videoInstructions = instructions.VideoStreamEncodingInstructions;

            // Format should hopefully always add space to end of append
            const string format = "{0} ";
            StringBuilder sbArguments = new();
            sbArguments.AppendFormat(format, $"-y -nostdin -i \"{SourceFullPath}\"");

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

            sbArguments.Append($"-max_muxing_queue_size 9999 -metadata title=\"{Name}\" \"{DestinationFullPath}\"");

            EncodingCommandArguments = new EncodingCommandArguments(false, sbArguments.ToString());
        }

        void BuildDolbyVisionEncodingCommandArguments()
        {
            const string format = "{0} ";
            string encodedVideoFullPath = EncodingInstructions.EncodedVideoFullPath;
            SourceStreamData streamData = SourceStreamData;

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

            VideoStreamEncodingInstructions videoInstructions = EncodingInstructions.VideoStreamEncodingInstructions;
            HDRData hdr = streamData.VideoStream.HDRData;
            videoInstructions.DynamicHDRMetadataFullPaths.TryGetValue(HDRFlags.DOLBY_VISION, out string dolbyVisionMetadataPath);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ffmpegFormatted = $"'{Path.Combine(State.FFmpegDirectory, "ffmpeg")}'";
                sourceFormatted = $"'{SourceFullPath.Replace("'", "'\\''")}'";
                x265Formatted = $"'{State.X265FullPath}'";
                outputFormatted = $"'{encodedVideoFullPath}'";
                masterDisplayFormatted = $"'G({hdr.Green_X},{hdr.Green_Y})B({hdr.Blue_X},{hdr.Blue_Y})R({hdr.Red_X},{hdr.Red_Y})WP({hdr.WhitePoint_X},{hdr.WhitePoint_Y})L({hdr.MaxLuminance},{hdr.MinLuminance})'";
                maxCLLFormatted = $"'{streamData.VideoStream.HDRData.MaxCLL}'";
                dolbyVisionPathFormatted = $"'{dolbyVisionMetadataPath}'";
            }
            else
            {
                ffmpegFormatted = $"\"{Path.Combine(State.FFmpegDirectory, "ffmpeg")}\"";
                sourceFormatted = $"\"{SourceFullPath}\"";
                x265Formatted = $"\"{State.X265FullPath}\"";
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
            sbAudioSubs.AppendFormat(format, $"-y -nostdin -i \"{SourceFullPath}\" -vn");
            foreach (AudioStreamEncodingInstructions audioInstructions in EncodingInstructions.AudioStreamEncodingInstructions)
            {
                sbAudioSubs.AppendFormat(format, $"-map 0:a:{audioInstructions.SourceIndex}");
            }
            foreach (SubtitleStreamEncodingInstructions subtitleInstructions in EncodingInstructions.SubtitleStreamEncodingInstructions)
            {
                sbAudioSubs.AppendFormat(format, $"-map 0:s:{subtitleInstructions.SourceIndex}");
            }

            for (int i = 0; i < EncodingInstructions.AudioStreamEncodingInstructions.Count; i++)
            {
                AudioStreamEncodingInstructions audioInstruction = EncodingInstructions.AudioStreamEncodingInstructions[i];
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

            for (int i = 0; i < EncodingInstructions.SubtitleStreamEncodingInstructions.Count; i++)
            {
                SubtitleStreamEncodingInstructions subtitleInstruction = EncodingInstructions.SubtitleStreamEncodingInstructions[i];
                if (subtitleInstruction.Forced is true)
                {
                    sbAudioSubs.AppendFormat(format, $"-c:s:{i} copy -disposition:s:{i} forced");
                }
                else
                {
                    sbAudioSubs.AppendFormat(format, $"-c:s:{i} copy");
                }
            }

            sbAudioSubs.AppendFormat(format, $"-max_muxing_queue_size 9999 \"{EncodingInstructions.EncodedAudioSubsFullPath}\"");
            audioSubEncodingCommandArguments = sbAudioSubs.ToString();

            // Merging
            StringBuilder sbMerge = new();
            sbMerge.AppendFormat(format, $"-o \"{DestinationFullPath}\" --compression -1:none \"{encodedVideoFullPath}\" --compression -1:none \"{EncodingInstructions.EncodedAudioSubsFullPath}\"")
                .Append($"--title \"{Name}\"");
            mergeCommandArguments = sbMerge.ToString();

            EncodingCommandArguments = new EncodingCommandArguments(true, videoEncodingCommandArguments, audioSubEncodingCommandArguments, mergeCommandArguments);
        }
        #endregion Local Functions
    }
}
