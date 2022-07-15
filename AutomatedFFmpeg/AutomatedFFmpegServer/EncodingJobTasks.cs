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
using System.Text;
using System.Threading;

namespace AutomatedFFmpegServer
{
    public static class EncodingJobTasks
    {
        public static void BuildEncodingJob(EncodingJob job, string ffmpegDir, Logger logger, CancellationToken cancellationToken)
        {
            job.Status = EncodingJobStatus.ANALYZING;

            CheckForCancellation(cancellationToken, job);

            // STEP 1: Initial ffprobe
            try
            {
                ProbeData probeData = GetProbeData(job.SourceFullPath, ffmpegDir);

                if (probeData is not null)
                {
                    // TODO: Convert to SourceFileData
                }
                else
                {
                    // Reset job status and exit
                    // TODO: Log error message
                    ResetJobStatus(job);
                    return;
                }
            }
            catch (Exception ex)
            {
                // TODO: Log Error
                ResetJobStatus(job);
                Debug.WriteLine(ex.Message);
                return;
            }

            CheckForCancellation(cancellationToken, job);

            // STEP 2: Get ScanType
            try
            {
                job.SourceFileData.VideoStream.ScanType = GetVideoScan(job.SourceFullPath);
            }
            catch (Exception ex)
            {
                // TODO: Log Error
                ResetJobStatus(job);
                Debug.WriteLine($"Error getting crop: {ex.Message}");
                return;
            }

            CheckForCancellation(cancellationToken, job);

            // STEP 3: Decide Encoding options / Determine Crop
            try
            {
                string crop = GetCrop(job.SourceFullPath, job.SourceFileData.DurationInSeconds / 2);
            }
            catch (Exception ex)
            {
                // TODO: Log Error
                ResetJobStatus(job);
                Debug.WriteLine($"Error getting crop: {ex.Message}");
                return;
            }

            CheckForCancellation(cancellationToken, job);

            // STEP 4: Create FFMPEG command

            job.Status = EncodingJobStatus.ANALYZED;
        }

        public static void Encode(EncodingJob job, string ffmpegDir, Logger logger, CancellationToken cancellationToken)
        {
            job.Status = EncodingJobStatus.ENCODING;

            CheckForCancellation(cancellationToken, job);

            job.Status = EncodingJobStatus.COMPLETE;
        }

        #region PRIVATE FUNCTIONS
        private static void CheckForCancellation(CancellationToken cancellationToken, EncodingJob job, [CallerMemberName] string callingFunctionName = "")
        {
            if (cancellationToken.IsCancellationRequested)
            {
                // Reset Status
                ResetJobStatus(job);
                // TODO: Log Cancel
                Console.WriteLine($"{callingFunctionName} was cancelled for {job}");
                return;
            }
        }

        private static void ResetJobStatus(EncodingJob job) => job.Status = job.Status.Equals(EncodingJobStatus.ANALYZING) ? EncodingJobStatus.NEW : EncodingJobStatus.ANALYZED;

        private static ProbeData GetProbeData(string sourceFullPath, string ffmpegDir)
        {
            string ffprobeArgs = $"-v quiet -read_intervals \"%+#2\" -print_format json -show_format -show_streams -show_entries frame \"{sourceFullPath}\"";

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = $@"{ffmpegDir.RemoveEndingSlashes()}\ffprobe.exe",
                Arguments = ffprobeArgs,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            StringBuilder sbFfprobeOutput = new StringBuilder();

            using (Process ffprobeProcess = new Process())
            {
                ffprobeProcess.StartInfo = startInfo;
                ffprobeProcess.Start();

                using (StreamReader reader = ffprobeProcess.StandardOutput)
                {
                    while (reader.Peek() >= 0)
                    {
                        sbFfprobeOutput.Append(reader.ReadLine());
                    }
                }

                ffprobeProcess.WaitForExit();
            }

            return JsonConvert.DeserializeObject<ProbeData>(sbFfprobeOutput.ToString());
        }

        private static string GetCrop(string sourceFullPath, int halfwayInSeconds)
        {
            string crop = string.Empty;
            string ffprobeArgs = $"-i \"{sourceFullPath}\" -ss {HelperMethods.ConvertSecondsToTimestamp(halfwayInSeconds)} -t 00:02:00 -vf cropdetect -f null - 2>&1 | awk '/crop/ {{ print $NF }}' | tail -1";

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = "ffprobe.exe",
                Arguments = ffprobeArgs,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            StringBuilder sbCrop = new StringBuilder();

            using (Process ffprobeProcess = new Process())
            {
                ffprobeProcess.StartInfo = startInfo;
                ffprobeProcess.Start();

                using (StreamReader reader = ffprobeProcess.StandardOutput)
                {
                    while (reader.Peek() >= 0)
                    {
                        sbCrop.Append(reader.ReadLine());
                    }
                }

                ffprobeProcess.WaitForExit();

                crop = sbCrop.ToString().Trim(Environment.NewLine.ToCharArray());
            }

            return crop;
        }

        private static VideoScanType GetVideoScan(string sourceFullPath)
        {
            string ffprobeArgs = $"-filter:v idet -frames:v 10000 -an -f rawvideo -y /dev/null -i \"{sourceFullPath}\" 2>&1 | awk '/frame detection/ {{print $8, $10, $12}}'";

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = "ffprobe.exe",
                Arguments = ffprobeArgs,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            StringBuilder sbScan = new StringBuilder();

            using (Process ffprobeProcess = new Process())
            {
                ffprobeProcess.StartInfo = startInfo;
                ffprobeProcess.Start();

                using (StreamReader reader = ffprobeProcess.StandardOutput)
                {
                    while (reader.Peek() >= 0)
                    {
                        sbScan.Append(reader.ReadLine());
                    }
                }

                ffprobeProcess.WaitForExit();

                List<string> scan = sbScan.ToString().Trim(Environment.NewLine.ToCharArray()).Split(',').ToList();
                int[] frame_totals = new int[3];

                foreach (string frames in scan)
                {
                    string[] counts = frames.Split(' ');
                    // Should always be the order of: TFF, BFF, PROG
                    frame_totals[(int)VideoScanType.INTERLACED_TFF - 1] += Convert.ToInt32(counts[0]);
                    frame_totals[(int)VideoScanType.INTERLACED_BFF - 1] += Convert.ToInt32(counts[1]);
                    frame_totals[(int)VideoScanType.PROGRESSIVE - 1] += Convert.ToInt32(counts[2]);
                }

                return (VideoScanType)Array.IndexOf(frame_totals, frame_totals.Max());
            }
        }
        #endregion PRIVATE FUNCTIONS
    }
}
