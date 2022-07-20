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
                ProbeData probeData = GetProbeData(job.SourceFileData.SourceFullPath, ffmpegDir);

                if (probeData is not null)
                {
                    job.SourceFileData = probeData.ToSourceFileData();
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
                VideoScanType scanType = GetVideoScan(job.SourceFileData.SourceFullPath, ffmpegDir);

                if (scanType.Equals(VideoScanType.UNDETERMINED))
                {
                    //logger.LogError($"Failed to determine VideoScanType for {job.FileName}.");
                    ResetJobStatus(job);
                    return;
                }
                else
                {
                    job.SourceFileData.VideoStream.ScanType = scanType;
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
                string crop = GetCrop(job.SourceFileData.SourceFullPath, ffmpegDir, job.SourceFileData.DurationInSeconds / 2);

                if (string.IsNullOrWhiteSpace(crop))
                {
                    //logger.LogError($"Failed to determine crop for {job.FileName}");
                    ResetJobStatus(job);
                    return;
                }
                else
                {
                    job.SourceFileData.VideoStream.Crop = crop;
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
        #endregion PRIVATE FUNCTIONS
    }
}
