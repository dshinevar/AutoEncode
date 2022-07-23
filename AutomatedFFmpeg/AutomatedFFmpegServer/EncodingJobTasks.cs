using AutomatedFFmpegServer.Data;
using AutomatedFFmpegUtilities;
using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegUtilities.Enums;
using AutomatedFFmpegUtilities.Logger;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace AutomatedFFmpegServer
{
    public static class EncodingJobTasks
    {
        public static void BuildEncodingJob(EncodingJob job, string ffmpegDir, Logger logger, CancellationToken cancellationToken)
        {
            job.Status = EncodingJobStatus.ANALYZING;

            CheckForCancellation(cancellationToken, job, logger);

            // STEP 1: Initial ffprobe
            try
            {
                ProbeData probeData = GetProbeData(job.SourceFullPath, ffmpegDir);

                if (probeData is not null)
                {
                    job.SourceStreamData = probeData.ToSourceFileData();
                }
                else
                {
                    // Reset job status and exit
                    //logger.LogError($"Failed to get probe data for {job.FileName}");
                    ResetJobStatus(job);
                    return;
                }
            }
            catch (Exception ex)
            {
                //logger.LogException(ex, $"Error getting probe or source file data for {job.FileName}");
                ResetJobStatus(job);
                Debug.WriteLine(ex.Message);
                return;
            }

            CheckForCancellation(cancellationToken, job, logger);

            // STEP 2: Get ScanType
            try
            {
                VideoScanType scanType = GetVideoScan(job.SourceFullPath, ffmpegDir);

                if (scanType.Equals(VideoScanType.UNDETERMINED))
                {
                    //logger.LogError($"Failed to determine VideoScanType for {job.FileName}.");
                    ResetJobStatus(job);
                    return;
                }
                else
                {
                    job.SourceStreamData.VideoStream.ScanType = scanType;
                }
            }
            catch (Exception ex)
            {
                //logger.LogException(ex, $"Error determining VideoScanType for {job.FileName}");
                ResetJobStatus(job);
                Debug.WriteLine($"Error getting crop: {ex.Message}");
                return;
            }

            CheckForCancellation(cancellationToken, job, logger);

            // STEP 3: Determine Crop
            try
            {
                string crop = GetCrop(job.SourceFullPath, ffmpegDir, job.SourceStreamData.DurationInSeconds / 2);

                if (string.IsNullOrWhiteSpace(crop))
                {
                    //logger.LogError($"Failed to determine crop for {job.FileName}");
                    ResetJobStatus(job);
                    return;
                }
                else
                {
                    job.SourceStreamData.VideoStream.Crop = crop;
                }
            }
            catch (Exception ex)
            {
                //logger.LogException(ex, $"Error determining crop for {job.FileName}");
                ResetJobStatus(job);
                Debug.WriteLine($"Error getting crop: {ex.Message}");
                return;
            }

            CheckForCancellation(cancellationToken, job, logger);

            // STEP 4: Decide Encoding Options
            job.EncodingInstructions = DetermineEncodingInstructions(job.SourceStreamData);

            // STEP 5: Create FFMPEG command

            job.Status = EncodingJobStatus.ANALYZED;
        }

        public static void Encode(EncodingJob job, string ffmpegDir, Logger logger, CancellationToken cancellationToken)
        {
            job.Status = EncodingJobStatus.ENCODING;

            CheckForCancellation(cancellationToken, job, logger);

            job.Status = EncodingJobStatus.COMPLETE;
        }

        #region PRIVATE FUNCTIONS
        private static void CheckForCancellation(CancellationToken cancellationToken, EncodingJob job, Logger logger, [CallerMemberName] string callingFunctionName = "")
        {
            if (cancellationToken.IsCancellationRequested)
            {
                // Reset Status
                ResetJobStatus(job);
                //logger.LogInfo($"{callingFunctionName} was cancelled for {job}", callingMemberName: callingFunctionName);
                Console.WriteLine($"{callingFunctionName} was cancelled for {job}");
                return;
            }
        }

        private static void ResetJobStatus(EncodingJob job) => job.Status = job.Status.Equals(EncodingJobStatus.ANALYZING) ? EncodingJobStatus.NEW : EncodingJobStatus.ANALYZED;

        private static ProbeData GetProbeData(string sourceFullPath, string ffmpegDir)
        {
            string ffprobeArgs = $"-v quiet -read_intervals \"%+#2\" -print_format json -show_format -show_streams -show_entries frame \"{sourceFullPath}\"";

            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = $@"{ffmpegDir.RemoveEndingSlashes()}{Path.AltDirectorySeparatorChar}ffprobe",
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

        private static string GetCrop(string sourceFullPath, string ffmpegDir, int halfwayInSeconds)
        {
            string ffmpegArgs = $"-ss {HelperMethods.ConvertSecondsToTimestamp(halfwayInSeconds)} -t 00:05:00 -i \"{sourceFullPath}\" -vf cropdetect -f null -";

            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName =  $@"{ffmpegDir.RemoveEndingSlashes()}{Path.AltDirectorySeparatorChar}ffmpeg",
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

        private static VideoScanType GetVideoScan(string sourceFullPath, string ffmpegDir)
        {
            string ffmpegArgs = $"-filter:v idet -frames:v 10000 -an -f rawvideo -y {Lookups.NullLocation} -i \"{sourceFullPath}\"";

            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = $@"{ffmpegDir.RemoveEndingSlashes()}{Path.AltDirectorySeparatorChar}ffmpeg",
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

        private static EncodingInstructions DetermineEncodingInstructions(SourceStreamData streamData)
        {
            EncodingInstructions instructions = new();

            VideoStreamEncodingInstructions videoStreamEncodingInstructions = new()
            {
                VideoEncoder = streamData.VideoStream.ResoultionInt >= Lookups.MinX265ResolutionInt ? VideoEncoder.LIBX265 : VideoEncoder.LIBX264,
                BFrames = streamData.VideoStream.Animated is true ? 8 : 6,
                Deinterlace = !streamData.VideoStream.ScanType.Equals(VideoScanType.PROGRESSIVE),
                HasHDR = streamData.VideoStream.HDRData is not null,
                Crop = true
            };
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
                        Language = bestQualityAudioStream.Language
                    });
                }
                else
                {
                    audioInstructions.Add(new()
                    {
                        SourceIndex = bestQualityAudioStream.AudioIndex,
                        AudioCodec = AudioCodec.COPY,
                        Language = bestQualityAudioStream.Language
                    });

                    audioInstructions.Add(new()
                    {
                        SourceIndex = bestQualityAudioStream.AudioIndex,
                        AudioCodec = AudioCodec.AAC,
                        Language = bestQualityAudioStream.Language
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
                    Forced = stream.Forced
                });
            }

            instructions.SubtitleStreamEncodingInstructions = subtitleInstructions.OrderBy(x => x.Forced).ToList();

            return instructions;
        }

        private static string BuildFFmpegCommandArguments(EncodingInstructions instructions, SourceStreamData streamData, string sourceFullPath, string destinationFullpath)
        {
            VideoStreamEncodingInstructions videoInstructions = instructions.VideoStreamEncodingInstructions;

            // Format should hopefully always add space to end of append
            const string format = "{0} ";
            StringBuilder sbArguments = new StringBuilder();
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
                HDRData hdr = streamData.VideoStream.HDRData;
                sbArguments.AppendFormat(format, "-vcodec libx265");
                if (!string.IsNullOrWhiteSpace(videoFilter)) sbArguments.AppendFormat(format, videoFilter);
                sbArguments.Append($"-x265-params \"preset=slow:keyint=60:bframes={videoInstructions.BFrames}:repeat-headers=1:")
                    .Append($"colorprim={streamData.VideoStream.ColorPrimaries}:transfer={streamData.VideoStream.ColorTransfer}:colormatrix={streamData.VideoStream.ColorSpace}")
                    .Append($"{(videoInstructions.HasHDR is true ? $":hdr10-opt=1:master-display='G({hdr.Green_X},{hdr.Green_Y})B({hdr.Blue_X},{hdr.Blue_Y})R({hdr.Red_X},{hdr.Red_Y})WP({hdr.WhitePoint_X},{hdr.WhitePoint_Y})L({hdr.MaxLuminance},{hdr.MinLuminance})'" : string.Empty)}")
                    .Append($"{(string.IsNullOrWhiteSpace(streamData.VideoStream.MaxCLL) ? string.Empty : $":max-cll={streamData.VideoStream.MaxCLL}")}")
                    .Append($"{(streamData.VideoStream.ChromaLocation is null ? string.Empty : $":chromaloc={(int)streamData.VideoStream.ChromaLocation}")}")
                    .Append("\"").AppendFormat(format, $"-crf {videoInstructions.CRF}");
            }
            else if (videoInstructions.VideoEncoder.Equals(VideoEncoder.LIBX264))
            {
                sbArguments.AppendFormat(format, "-vcodec libx264");
                if (!string.IsNullOrWhiteSpace(videoFilter)) sbArguments.AppendFormat(format, videoFilter);
                sbArguments.AppendFormat(format, $"-x264-params \"preset=veryslow:bframes=16:b-adapt=2:b-pyramid=normal:partitions=all\" -crf {videoInstructions.CRF}");
            }
            else
            {
                throw new Exception("Unknown VideoEncoder. Unable to build ffmpeg arguments.");
            }

            // Audio Section

        }
        #endregion PRIVATE FUNCTIONS
    }
}
