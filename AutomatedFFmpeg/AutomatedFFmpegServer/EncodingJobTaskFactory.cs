using AutomatedFFmpegServer.Data;
using AutomatedFFmpegUtilities;
using AutomatedFFmpegUtilities.Config;
using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegUtilities.Enums;
using AutomatedFFmpegUtilities.Logger;
using AutomatedFFmpegUtilities.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Runtime.InteropServices;

namespace AutomatedFFmpegServer
{
    public static class EncodingJobTaskFactory
    {
        /// <summary>Builds out an <see cref="EncodingJob"/> by analyzing the file's streams, building encoding instructions, and building FFmpeg arguments.</summary>
        /// <param name="job">The <see cref="EncodingJob"/> to be filled out.</param>
        /// <param name="ffmpegDir">The directory ffmpeg/ffprobe is located in.</param>
        /// <param name="hdr10plusExtractorPath">The full path of the hdr10plus extractor program (hdr10plus_tool)</param>
        /// <param name="dolbyVisionExtractorPath">The full path of the dolby vision extractor program (dovi_tool)</param>
        /// <param name="logger"><see cref="Logger"/></param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        public static void BuildEncodingJob(EncodingJob job, string ffmpegDir, string hdr10plusExtractorPath, string dolbyVisionExtractorPath, Logger logger, CancellationToken cancellationToken)
        {
            job.Status = EncodingJobStatus.BUILDING;

            CheckForCancellation(job, logger, cancellationToken);

            // STEP 1: Initial ffprobe
            try
            {
                ProbeData probeData = GetProbeData(job.SourceFullPath, ffmpegDir);

                if (probeData is not null)
                {
                    job.SourceStreamData = probeData.ToSourceStreamData();
                }
                else
                {
                    // Reset job status and exit
                    string msg = $"Failed to get probe data for {job.FileName}";
                    logger.LogError(msg);
                    Debug.WriteLine(msg);
                    job.SetError(msg);
                    return;
                }
            }
            catch (Exception ex)
            {
                string msg = $"Error getting probe or source file data for {job.FileName}";
                logger.LogException(ex, msg);
                Debug.WriteLine($"{msg} : {ex.Message}");
                job.SetError(msg);
                return;
            }

            if (CheckForCancellation(job, logger, cancellationToken)) return;

            // STEP 2: Get ScanType
            try
            {
                VideoScanType scanType = GetVideoScan(job.SourceFullPath, ffmpegDir);

                if (scanType.Equals(VideoScanType.UNDETERMINED))
                {
                    string msg = $"Failed to determine VideoScanType for {job.FileName}.";
                    logger.LogError(msg);
                    Debug.WriteLine(msg);
                    job.SetError(msg);
                    return;
                }
                else
                {
                    job.SourceStreamData.VideoStream.ScanType = scanType;
                }
            }
            catch (Exception ex)
            {
                string msg = $"Error determining VideoScanType for {job.FileName}";
                logger.LogException(ex, msg);
                Debug.WriteLine($"{msg} : {ex.Message}");
                job.SetError(msg);
                return;
            }

            if (CheckForCancellation(job, logger, cancellationToken)) return;

            // STEP 3: Determine Crop
            try
            {
                string crop = GetCrop(job.SourceFullPath, ffmpegDir, job.SourceStreamData.DurationInSeconds);

                if (string.IsNullOrWhiteSpace(crop))
                {
                    string msg = $"Failed to determine crop for {job.FileName}";
                    logger.LogError(msg);
                    job.SetError(msg);
                    return;
                }
                else
                {
                    job.SourceStreamData.VideoStream.Crop = crop;
                }
            }
            catch (Exception ex)
            {
                string msg = $"Error determining crop for {job.FileName}";
                logger.LogException(ex, msg);
                Debug.WriteLine($"{msg} : {ex.Message}");
                job.SetError(msg);
                return;
            }

            if (CheckForCancellation(job, logger, cancellationToken)) return;

            // OPTIONAL STEP: Create HDR metadata file if needed
            try
            {
                if (job.SourceStreamData?.VideoStream?.IsDynamicHDR ?? false)
                {
                    HDRType hdrType = job.SourceStreamData.VideoStream.HDRData.HDRType;
                    if (hdrType.Equals(HDRType.HDR10PLUS))
                    {
                        // If we aren't given a path, skip this step;  It will be treated as HDR10
                        if (!string.IsNullOrWhiteSpace(hdr10plusExtractorPath))
                        {
                            ((IDynamicHDRData)job.SourceStreamData.VideoStream.HDRData).MetadataFullPath = CreateHDRMetadataFile(job.SourceFullPath, hdrType, ffmpegDir, hdr10plusExtractorPath);
                        }
                        else
                        {
                            logger.LogWarning($"No HDR Metadata Extractor given for {job.Name}. Will be treated as HDR10 instead of Dynamic HDR.");
                            Debug.WriteLine($"No HDR Metadata Extractor given for {job.Name}. Will be treated as HDR10 instead of Dynamic HDR.");
                        }
                    }
                    else if (hdrType.Equals(HDRType.DOLBY_VISION))
                    {
                        // If we aren't given a path, skip this step;  It will be treated as HDR10
                        if (!string.IsNullOrWhiteSpace(dolbyVisionExtractorPath))
                        {
                            ((IDynamicHDRData)job.SourceStreamData.VideoStream.HDRData).MetadataFullPath = CreateHDRMetadataFile(job.SourceFullPath, hdrType, ffmpegDir, dolbyVisionExtractorPath);
                        }
                        else
                        {
                            logger.LogWarning($"No HDR Metadata Extractor given for {job.Name}. Will be treated as HDR10 instead of Dynamic HDR.");
                            Debug.WriteLine($"No HDR Metadata Extractor given for {job.Name}. Will be treated as HDR10 instead of Dynamic HDR.");
                        }
                    }
                    else
                    {
                        throw new Exception("Unsupported or unknown HDRType.");
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = $"Error creating HDR metadata file for {job.FileName}";
                logger.LogException(ex, msg);
                Debug.WriteLine($"{msg} : {ex.Message}");
                job.SetError(msg);
                return;
            }

            // STEP 4: Decide Encoding Options
            // Not sure what would throw an exception but we'll wrap in try/catch just in case.
            try
            {
                job.EncodingInstructions = DetermineEncodingInstructions(job.SourceStreamData);
            }
            catch (Exception ex)
            {
                string msg = $"Error building encoding instructions for {job.FileName}";
                logger.LogException(ex, msg);
                Debug.WriteLine($"{msg} : {ex.Message}");
                job.SetError(msg);
                return;
            }

            if (CheckForCancellation(job, logger, cancellationToken)) return;

            // STEP 5: Create FFMPEG command
            try
            {
                job.FFmpegEncodingCommandArguments = BuildFFmpegCommandArguments(job.EncodingInstructions, job.SourceStreamData, job.Name, job.SourceFullPath, job.DestinationFullPath);
            }
            catch (Exception ex)
            {
                string msg = $"Error building FFmpeg command for {job.FileName}";
                logger.LogException(ex, msg);
                Debug.WriteLine($"{msg} : {ex.Message}");
                job.SetError(msg);
                return;
            }

            job.Status = EncodingJobStatus.BUILT;
            logger.LogInfo($"Successfully built {job.Name} encoding job.");
        }

        /// <summary> Calls ffmpeg to do encoding; Handles output from ffmpeg </summary>
        /// <param name="job">The <see cref="EncodingJob"/> to be encoded.</param>
        /// <param name="ffmpegDir">The directory ffmpeg/ffprobe is located in.</param>
        /// <param name="logger"><see cref="Logger"/></param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        public static void Encode(EncodingJob job, string ffmpegDir, Logger logger, CancellationToken cancellationToken)
        {
            job.Status = EncodingJobStatus.ENCODING;

            if (CheckForCancellation(job, logger, cancellationToken)) return;

            Stopwatch stopwatch = new();
            try
            {
                ProcessStartInfo startInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = Path.Combine(ffmpegDir, "ffmpeg"),
                    Arguments = job.FFmpegEncodingCommandArguments,
                    UseShellExecute = false,
                    RedirectStandardError = true
                };

                int count = 0;

                using (Process ffmpegProcess = new())
                {
                    ffmpegProcess.StartInfo = startInfo;
                    ffmpegProcess.ErrorDataReceived += (sender, e) =>
                    {
                        Process proc = sender as Process;
                        if (cancellationToken.IsCancellationRequested is true)
                        {
                            job.EncodingProgress = 0;
                            job.ResetStatus();
                            proc.CancelErrorRead();
                            proc.Close();
                            return;
                        }

                        job.ElapsedEncodingTime = stopwatch.Elapsed;

                        // Only check output every 50 events, don't need to do this frequently
                        if (count >= 50)
                        {
                            if (string.IsNullOrWhiteSpace(e.Data) is false && e.Data.Contains("time="))
                            {
                                string line = e.Data;
                                string time = line.Substring(line.IndexOf("time="), 13);
                                int seconds = HelperMethods.ConvertTimestampToSeconds(time.Split('=')[1]);
                                job.EncodingProgress = (int)(((double)seconds / (double)job.SourceStreamData.DurationInSeconds) * 100); // Update percent complete
                                Debug.WriteLine(job.EncodingProgress);
                            }
                            count = 0;
                        }
                        else
                        {
                            count++;
                        }
                    };
                    ffmpegProcess.Exited += (sender, e) =>
                    {
                        Process proc = sender as Process;
                        if (proc.ExitCode != 0)
                        {
                            File.Delete(job.DestinationFullPath);
                            string msg = $"Encoding process for {job.Name} ended unsuccessfully.";
                            job.SetError(msg);
                            logger.LogError(msg);
                        }
                    };

                    File.WriteAllText(Lookups.PreviouslyEncodingTempFile, job.DestinationFullPath);
                    stopwatch.Start();
                    ffmpegProcess.Start();
                    ffmpegProcess.BeginErrorReadLine();
                    ffmpegProcess.WaitForExit();
                }    
            }
            catch (Exception ex)
            {
                string msg = $"Error encoding {job.FileName}.";
                logger.LogException(ex, msg);
                Debug.WriteLine($"{msg} : {ex.Message}");
                job.SetError(msg);
            }

            stopwatch.Stop();

            try
            {
                // SUCCESS
                if (job.EncodingProgress >= 75)
                {
                    job.CompleteEncoding(stopwatch.Elapsed);
                    File.Delete(Lookups.PreviouslyEncodingTempFile);
                    if (job.SourceStreamData.VideoStream.IsDynamicHDR) File.Delete(((IDynamicHDRData)job.SourceStreamData.VideoStream.HDRData).MetadataFullPath);
                    logger.LogInfo($"Successfully encoded {job.Name}. Estimated Time Elapsed: {stopwatch.Elapsed:hh\\:mm\\:ss}");
                }
                // FAILURE
                else if (job.EncodingProgress < 75 || job.Error is true)
                {
                    // Go ahead and clear out the temp file AND the encoded file (most likely didn't finish)
                    File.Delete(job.DestinationFullPath);
                    File.Delete(Lookups.PreviouslyEncodingTempFile);
                    job.ResetEncoding();
                }
            }
            catch (Exception ex)
            {
                // Most likely an exception from File.Delete
                string msg = $"Error cleaning up encoding job.";
                logger.LogException(ex, msg);
                Debug.WriteLine($"{msg} : {ex.Message}");
                // Don't error the job for now
            }
        }

        /// <summary> Runs post-processing tasks marked for the encoding job. </summary>
        /// <param name="job">The <see cref="EncodingJob"/> to be post-processed.</param>
        /// <param name="logger"><see cref="Logger"/></param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        public static void EncodingJobPostProcessing(EncodingJob job, Logger logger, CancellationToken cancellationToken)
        {
            // Double-check to ensure we don't post-process a job that shouldn't be
            if (job.PostProcessingFlags.Equals(PostProcessingFlags.None)) return;

            job.Status = EncodingJobStatus.POST_PROCESSING;

            if (CheckForCancellation(job, logger, cancellationToken)) return;

            // COPY FILES
            if (job.PostProcessingFlags.HasFlag(PostProcessingFlags.Copy))
            {
                try
                {
                    foreach (string path in job.PostProcessingSettings.CopyFilePaths)
                    {
                        File.Copy(job.DestinationFullPath, Path.Combine(path, Path.GetFileName(job.DestinationFullPath)), true);
                    }
                }
                catch (Exception ex)
                {
                    string msg = $"Error copying output file to other locations for {job.Name}";
                    logger.LogException(ex, msg);
                    Debug.WriteLine($"{msg} : {ex.Message}");
                    job.SetError(msg);
                    return;
                }
            }

            if (CheckForCancellation(job, logger, cancellationToken)) return;

            // DELETE SOURCE FILE
            if (job.PostProcessingFlags.HasFlag(PostProcessingFlags.DeleteSourceFile))
            {
                try
                {
                    File.Delete(job.SourceFullPath);
                }
                catch (Exception ex)
                {
                    string msg = $"Error deleting source file for {job.Name}";
                    logger.LogException(ex, msg);
                    Debug.WriteLine($"{msg} : {ex.Message}");
                    job.SetError(msg);
                    return;
                }
            }

            job.CompletePostProcessing();
            logger.LogInfo($"Successfully post-processed {job.Name} encoding job.");
        }

        #region General Private Functions
        /// <summary>Checks for a cancellation token. Returns true if task was cancelled. </summary>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <param name="job"><see cref="EncodingJob"/> whose status will be reset if cancelled.</param>
        /// <param name="logger"><see cref="Logger"/></param>
        /// <param name="callingFunctionName">Calling method name.</param>
        /// <returns>True if cancelled; False otherwise.</returns>
        private static bool CheckForCancellation(EncodingJob job, Logger logger, CancellationToken cancellationToken, [CallerMemberName] string callingFunctionName = "")
        {
            bool cancel = false;
            if (cancellationToken.IsCancellationRequested)
            {
                // Reset Status
                job.ResetStatus();
                logger.LogInfo($"{callingFunctionName} was cancelled for {job}", callingMemberName: callingFunctionName);
                Debug.WriteLine($"{callingFunctionName} was cancelled for {job}");
                cancel = true;
            }
            return cancel;
        }
        #endregion General Private Functions

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

        /// <summary> Gets crop of source file by determining the most frequently detected crop by ffmpeg. </summary>
        /// <param name="sourceFullPath">Full path of source file.</param>
        /// <param name="ffmpegDir">Directory FFmpeg is located in</param>
        /// <param name="duration">Duration in seconds of file.</param>
        /// <returns>String of crop in this format: "crop=XXXX:YYYY:AA:BB"</returns>
        private static string GetCrop(string sourceFullPath, string ffmpegDir, int duration)
        {
            int halfwayInSeconds = duration / 2;
            string ffmpegArgs = $"-ss {HelperMethods.ConvertSecondsToTimestamp(halfwayInSeconds)} -t 00:05:00 -i \"{sourceFullPath}\" -vf cropdetect -f null -";

            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = Path.Combine(ffmpegDir, "ffmpeg"),
                Arguments = ffmpegArgs,
                UseShellExecute = false,
                RedirectStandardError = true
            };

            StringBuilder sbCrop = new();

            using (Process ffmpegProcess = new())
            {
                ffmpegProcess.StartInfo = startInfo;
                ffmpegProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (string.IsNullOrWhiteSpace(e.Data) is false && e.Data.Contains("crop=")) sbCrop.AppendLine(e.Data);
                };
                ffmpegProcess.Start();
                ffmpegProcess.BeginErrorReadLine();
                ffmpegProcess.WaitForExit();
            }

            IEnumerable<string> cropLines = sbCrop.ToString().TrimEnd(Environment.NewLine.ToCharArray()).Split(Environment.NewLine);
            List<string> crops = new();
            foreach (string line in cropLines)
            {
                crops.Add(line[line.IndexOf("crop=")..]);
            }
            // Grab most frequent crop
            return crops.GroupBy(x => x).MaxBy(y => y.Count()).Key;
        }

        /// <summary> Gets the <see cref="VideoScanType"/> of the file. </summary>
        /// <param name="sourceFullPath">Full path of source file.</param>
        /// <param name="ffmpegDir">Directory FFmpeg is located in</param>
        /// <returns>The <see cref="VideoScanType"/> of the file.</returns>
        private static VideoScanType GetVideoScan(string sourceFullPath, string ffmpegDir)
        {
            string ffmpegArgs = $"-filter:v idet -frames:v 10000 -an -f rawvideo -y {Lookups.NullLocation} -i \"{sourceFullPath}\"";

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

            using (Process ffmpegProcess = new())
            {
                ffmpegProcess.StartInfo = startInfo;
                ffmpegProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (string.IsNullOrWhiteSpace(e.Data) is false && e.Data.Contains("frame detection")) sbScan.AppendLine(e.Data);
                };

                ffmpegProcess.Start();
                ffmpegProcess.BeginErrorReadLine();
                ffmpegProcess.WaitForExit();
            }

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
        /// <param name="hdrType"><see cref="HDRType"/></param>
        /// <param name="ffmpegDir"></param>
        /// <param name="extractorFullPath">Full path of dynamic hdr data extractor to use.</param>
        /// <returns>Full path of created metadata file.</returns>
        /// <exception cref="Exception">Thrown if metadata file not created.</exception>
        private static string CreateHDRMetadataFile(string sourceFullPath, HDRType hdrType, string ffmpegDir, string extractorFullPath)
        {
            string metadataOutputFile = $"{Path.GetTempPath()}{Path.GetFileNameWithoutExtension(sourceFullPath).Replace('\'', ' ')}{(hdrType.Equals(HDRType.HDR10PLUS) ? ".json" : ".RPU.bin")}";

            string ffmpegArgs = string.Empty;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string extractorArgs = hdrType.Equals(HDRType.HDR10PLUS) ? $"'{extractorFullPath}' extract -o '{metadataOutputFile}' - " :
                                                                        $"'{extractorFullPath}' extract-rpu - -o '{metadataOutputFile}'";

                ffmpegArgs = $"-c \"{Path.Combine(ffmpegDir, "ffmpeg")} -nostdin -i '{sourceFullPath.Replace("'", "'\\''")}' -c:v copy -vbsf hevc_mp4toannexb -f hevc - | {extractorArgs}\"";
            }
            else
            {
                string extractorArgs = hdrType.Equals(HDRType.HDR10PLUS) ? $"\"{extractorFullPath}\" extract -o \"{metadataOutputFile}\" - " :
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

            using (Process ffmpegProcess = new())
            {
                ffmpegProcess.StartInfo = startInfo;
                ffmpegProcess.Start();
                ffmpegProcess.WaitForExit();
            }

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
        private static EncodingInstructions DetermineEncodingInstructions(SourceStreamData streamData)
        {
            EncodingInstructions instructions = new();

            VideoStreamEncodingInstructions videoStreamEncodingInstructions = new()
            {
                VideoEncoder = streamData.VideoStream.ResoultionInt >= Lookups.MinX265ResolutionInt ? VideoEncoder.LIBX265 : VideoEncoder.LIBX264,
                BFrames = streamData.VideoStream.Animated is true ? 8 : 6,
                Deinterlace = !streamData.VideoStream.ScanType.Equals(VideoScanType.PROGRESSIVE),
                Crop = true
            };

            if (streamData.VideoStream.IsDynamicHDR)
            {
                // If the data is dynamic hdr but we don't have a metadata file, treat as normal HDR10
                if (string.IsNullOrWhiteSpace(((IDynamicHDRData)streamData.VideoStream.HDRData).MetadataFullPath))
                {
                    videoStreamEncodingInstructions.HDRType = HDRType.HDR10;
                }
                else
                {
                    videoStreamEncodingInstructions.HDRType = streamData.VideoStream.HDRData.HDRType;
                    videoStreamEncodingInstructions.DynamicHDRMetadataFullPath = ((IDynamicHDRData)streamData.VideoStream.HDRData).MetadataFullPath;
                }
            }
            else
            {
                videoStreamEncodingInstructions.HDRType = streamData.VideoStream.IsHDR ? streamData.VideoStream.HDRData.HDRType : HDRType.NONE;
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
                .ThenByDescending(x => x.AudioCodec.Equals(AudioCodec.COPY))
                .ToList(); // Put COPY before anything else

            List<SubtitleStreamEncodingInstructions> subtitleInstructions = new();
            foreach (SubtitleStreamData stream in streamData.SubtitleStreams)
            {
                subtitleInstructions.Add(new()
                {
                    SourceIndex = stream.SubtitleIndex,
                    Forced = stream.Forced,
                    Title = stream.Title
                });
            }

            instructions.SubtitleStreamEncodingInstructions = subtitleInstructions.OrderBy(x => x.Forced).ToList();

            return instructions;
        }

        /// <summary> Builds the FFmpeg command arguments string </summary>
        /// <param name="instructions"><see cref="EncodingInstructions"/> data</param>
        /// <param name="streamData"><see cref="StreamData"/></param>
        /// <param name="title">The final title to set in the file metadata</param>
        /// <param name="sourceFullPath">Full path of the source file</param>
        /// <param name="destinationFullPath">Full path for the expected destination file</param>
        /// <returns>A string of the FFmpeg arguments</returns>
        /// <exception cref="Exception">Something went wrong/invalid instructions.</exception>
        /// <exception cref="NotImplementedException">Potentially unimplemented instructions.</exception>
        private static string BuildFFmpegCommandArguments(EncodingInstructions instructions, SourceStreamData streamData, string title, string sourceFullPath, string destinationFullPath)
        {
            VideoStreamEncodingInstructions videoInstructions = instructions.VideoStreamEncodingInstructions;

            // Format should hopefully always add space to end of append
            const string format = "{0} ";
            StringBuilder sbArguments = new();
            sbArguments.AppendFormat(format, $"-y -i \"{sourceFullPath}\"");
            
            // Map Section
            sbArguments.AppendFormat(format, "-map 0:v:0");
            foreach (AudioStreamEncodingInstructions audioInstructions in instructions.AudioStreamEncodingInstructions)
            {
                sbArguments.AppendFormat(format, $"-map 0:a:{audioInstructions.SourceIndex}");
            }
            foreach (SubtitleStreamEncodingInstructions subtitleInstructions in instructions.SubtitleStreamEncodingInstructions)
            {
                sbArguments.AppendFormat(format , $"-map 0:s:{subtitleInstructions.SourceIndex}");
            }

            // Video Section
            string deinterlace = videoInstructions.Deinterlace is true ? $"yadif=1:{(int)streamData.VideoStream.ScanType}:0" : string.Empty;
            string crop = videoInstructions.Crop is true ? streamData.VideoStream.Crop : string.Empty;
            string videoFilter = string.Empty;

            if (!string.IsNullOrWhiteSpace(deinterlace) || !string.IsNullOrWhiteSpace(crop))
            {
                videoFilter = $"-vf \"{HelperMethods.JoinFilter(":", crop, deinterlace)}\"";
            }

            sbArguments.AppendFormat(format, $"-pix_fmt {videoInstructions.PixelFormat}");
            if (videoInstructions.VideoEncoder.Equals(VideoEncoder.LIBX265))
            {
                IHDRData hdr = streamData.VideoStream.HDRData;
                sbArguments.AppendFormat(format, "-vcodec libx265").AppendFormat(format, $"-crf {videoInstructions.CRF}");
                if (!string.IsNullOrWhiteSpace(videoFilter)) sbArguments.AppendFormat(format, videoFilter);
                sbArguments.Append($"-x265-params \"preset=slow:keyint=60:bframes={videoInstructions.BFrames}:repeat-headers=1:")
                    .Append($"colorprim={streamData.VideoStream.ColorPrimaries}:transfer={streamData.VideoStream.ColorTransfer}:colormatrix={streamData.VideoStream.ColorSpace}")
                    .Append($"{(streamData.VideoStream.ChromaLocation is null ? string.Empty : $":chromaloc={(int)streamData.VideoStream.ChromaLocation}")}");

                if (videoInstructions.HasHDR)
                {
                    // HDR10 data; Always add
                    sbArguments.Append($":master-display='G({hdr.Green_X},{hdr.Green_Y})B({hdr.Blue_X},{hdr.Blue_Y})R({hdr.Red_X},{hdr.Red_Y})WP({hdr.WhitePoint_X},{hdr.WhitePoint_Y})L({hdr.MaxLuminance},{hdr.MinLuminance})':max-cll={streamData.VideoStream.HDRData.MaxCLL}");
                    switch (videoInstructions.HDRType)
                    {
                        case HDRType.HDR10PLUS:
                        {
                            sbArguments.Append($":dhdr10-info='{videoInstructions.DynamicHDRMetadataFullPath}'");
                            break;
                        }
                        case HDRType.DOLBY_VISION:
                        {
                            sbArguments.Append($":dolby-vision-rpu={videoInstructions.DynamicHDRMetadataFullPath}:dolby-vision-profile=8.1");
                            break;
                        }
                    }
                }
                sbArguments.AppendFormat(format, '"');
            }
            else if (videoInstructions.VideoEncoder.Equals(VideoEncoder.LIBX264))
            {
                sbArguments.AppendFormat(format, "-vcodec libx264");
                if (!string.IsNullOrWhiteSpace(videoFilter)) sbArguments.AppendFormat(format, videoFilter);
                sbArguments.AppendFormat(format, $"-x264-params \"preset=veryslow:bframes=16:b-adapt=2:b-pyramid=normal:partitions=all\" -crf {videoInstructions.CRF}");
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
                        .AppendFormat(format, $"-metadata:s:a:{i} title=\"Stereo ({audioInstruction.Language})\"");
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

            sbArguments.Append($"-max_muxing_queue_size 9999 -metadata title=\"{title}\" \"{destinationFullPath}\"");

            return sbArguments.ToString();
        }
        #endregion BuildEncodingJob Private Functions
    }
}
