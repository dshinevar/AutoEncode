using System;
using System.Threading;
using System.Diagnostics;
using AutomatedFFmpegUtilities.Data;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using AutomatedFFmpegServer.Data;
using AutomatedFFmpegUtilities.Enums;
using System.Collections.Generic;
using System.Linq;

public static class EncodingJobTasks
{
    public static void BuildEncodingJob(EncodingJob job, CancellationToken cancellationToken)
    {
        ProbeData probeData = null;
        job.Status = EncodingJobStatus.ANALYZING;

        if (cancellationToken.IsCancellationRequested)
        {
            // Reset Status
            job.Status = EncodingJobStatus.NEW;
            // TODO: Log Cancel
            Console.WriteLine($"{nameof(BuildEncodingJob)} was cancelled for {job.ToString()}");
            return;
        }    

        // STEP 1: Initial ffprobe
        try
        {
            string ffprobeArgs = $"-v quiet -read_intervals \"%+#2\" -print_format json -show_format -show_streams -show_entries frame \"{job.SourceFullPath}\"";

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = "ffprobe.exe",
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

            probeData = JsonConvert.DeserializeObject<ProbeData>(sbFfprobeOutput.ToString());
        }
        catch (Exception ex)
        {
            // TODO: Log Error
            Debug.WriteLine(ex.Message);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            // Reset Status
            job.Status = EncodingJobStatus.NEW;
            // TODO: Log Cancel
            Console.WriteLine($"{nameof(BuildEncodingJob)} was cancelled for {job.ToString()}");
            return;
        }

        // STEP 2: Get Crop and Scan
        // 2a: Crop
        try
        {
            string ffprobeArgs = $"-i \"{job.SourceFullPath}\" -ss %s -t 00:02:00 -vf cropdetect -f null - 2>&1 | awk '/crop/ {{ print $NF }}' | tail -1";

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

                string crop = sbCrop.ToString().Trim(Environment.NewLine.ToCharArray());
            }
        }
        catch (Exception ex)
        {
            // TODO: Log Error
            Debug.WriteLine($"Error getting crop: {ex.Message}");
        }

        // 2b: Scan
        try
        {
            string ffprobeArgs = $"-filter:v idet -frames:v 10000 -an -f rawvideo -y /dev/null -i \"{job.SourceFullPath}\" 2>&1 | awk '/frame detection/ {{print $8, $10, $12}}'";

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
		            frame_totals[(int)VideoScanType.INTERLACED_TFF] += Convert.ToInt32(counts[0]);
		            frame_totals[(int)VideoScanType.INTERLACED_BFF] += Convert.ToInt32(counts[1]);
		            frame_totals[(int)VideoScanType.PROGRESSIVE] += Convert.ToInt32(counts[2]);
                }

                VideoScanType scanType = (VideoScanType)Array.IndexOf(frame_totals, frame_totals.Max());
            }
        }
        catch (Exception ex)
        {
            // TODO: Log Error
            Debug.WriteLine($"Error getting crop: {ex.Message}");
        }
    }
}