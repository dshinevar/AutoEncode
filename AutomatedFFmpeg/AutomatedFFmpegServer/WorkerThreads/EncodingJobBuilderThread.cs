using AutomatedFFmpegServer.Base;
using AutomatedFFmpegUtilities.Config;
using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegUtilities.Enums;
using AutomatedFFmpegServer.Data;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO;
using System;
using System.Text;

namespace AutomatedFFmpegServer.WorkerThreads
{
    public class EncodingJobBuilderThread : AFWorkerThreadBase
    {
        private bool Shutdown { get; set; } = false;
        private int JobCheckCounter { get; set; } = 0;
        public EncodingJobBuilderThread(AFServerMainThread mainThread, AFServerConfig serverConfig)
            : base("EncodingJobBuilderThread", mainThread, serverConfig) { }

        public override void Stop()
        {
            Shutdown = true;
            base.Stop();
        }

        protected override void ThreadLoop(object[] objects = null)
        {
            while (Shutdown == false)
            {
                EncodingJob job = EncodingJobQueue.GetNextEncodingJobWithStatus(EncodingJobStatus.NEW);

                if (job != null)
                {
                    JobCheckCounter = 0;
                    job.Status = EncodingJobStatus.ANALYZING;

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
                    try
                    {
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

                        ProbeData probeData = JsonConvert.DeserializeObject<ProbeData>(sbFfprobeOutput.ToString());
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
                else
                {
                    JobCheckCounter++;
                    if (JobCheckCounter > 5)
                    {
                        // Checked multiple times for new job, just deep sleep
                        JobCheckCounter = 0;
                        DeepSleep();
                    }
                    else
                    {
                        // Quick sleep to see if a job appears
                        Sleep();
                    }
                }
            }
        }
    }
}
