using AutomatedFFmpegServer.Data;
using AutomatedFFmpegUtilities;
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
    public static class EncodingTaskFactory
    {
        /// <summary> Calls ffmpeg to do encoding; Handles output from ffmpeg </summary>
        /// <param name="job">The <see cref="EncodingJob"/> to be encoded.</param>
        /// <param name="ffmpegDir">The directory ffmpeg/ffprobe is located in.</param>
        /// <param name="logger"><see cref="Logger"/></param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        public static void Encode(EncodingJob job, string ffmpegDir, Logger logger, CancellationToken cancellationToken)
        {
            job.Status = EncodingJobStatus.ENCODING;

            if (ServerMethods.CheckForCancellation(job, logger, cancellationToken)) return;

            Stopwatch stopwatch = new();
            try
            {
                ProcessStartInfo startInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = Path.Combine(ffmpegDir, "ffmpeg"),
                    Arguments = ((EncodingCommandArguments)job.EncodingCommandArguments).FFmpegEncodingCommandArguments,
                    UseShellExecute = false,
                    RedirectStandardError = true
                };

                int count = 0;

                using (Process ffmpegProcess = new())
                {
                    ffmpegProcess.StartInfo = startInfo;
                    ffmpegProcess.EnableRaisingEvents = true;
                    ffmpegProcess.ErrorDataReceived += (sender, e) =>
                    {
                        Process proc = sender as Process;
                        if (cancellationToken.IsCancellationRequested is true)
                        {
                            proc.CancelErrorRead();
                            proc.Kill();
                            return;
                        }
                        job.ElapsedEncodingTime = stopwatch.Elapsed;

                        if (string.IsNullOrWhiteSpace(e.Data) is false)
                        {
                            HandleEncodingOutput(e.Data, job, ref count);
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
                string msg = $"Error encoding {job}.";
                logger.LogException(ex, msg);
                Debug.WriteLine($"{msg} : {ex.Message}");
                job.SetError(msg);
            }

            stopwatch.Stop();

            try
            {
                // SUCCESS
                if (job.EncodingProgress >= 75 && job.Error is false)
                {
                    job.CompleteEncoding(stopwatch.Elapsed);
                    File.Delete(Lookups.PreviouslyEncodingTempFile);
                    if (job.EncodingInstructions.VideoStreamEncodingInstructions.HasDynamicHDR is true)
                    {
                        // Delete all possible HDRMetadata files
                        ((IDynamicHDRData)job.SourceStreamData.VideoStream.HDRData).MetadataFullPaths.Select(x => x.Value).ToList().ForEach(y => File.Delete(y));
                    }
                    logger.LogInfo($"Successfully encoded {job}. Estimated Time Elapsed: {stopwatch.Elapsed:hh\\:mm\\:ss}");
                }
                // CANCELLED
                else if (cancellationToken.IsCancellationRequested is true)
                {
                    // Go ahead and clear out the temp file AND the encoded file (most likely didn't finish)
                    File.Delete(job.DestinationFullPath);
                    File.Delete(Lookups.PreviouslyEncodingTempFile);
                    job.ResetEncoding();
                    logger.LogError($"{job} was cancelled.");
                }
                // DIDN'T FINISH BUT DIDN'T RECEIVE ERROR
                else if (job.Error is false && job.EncodingProgress < 75)
                {
                    // Go ahead and clear out the temp file AND the encoded file (most likely didn't finish)
                    File.Delete(job.DestinationFullPath);
                    File.Delete(Lookups.PreviouslyEncodingTempFile);
                    job.SetError($"{job} encoding job ended prematurely.");
                    logger.LogError($"{job} encoding job ended prematurely.");
                }
                // JOB ERRORED
                else if (job.Error is true)
                {
                    // Go ahead and clear out the temp file AND the encoded file (most likely didn't finish)
                    File.Delete(job.DestinationFullPath);
                    File.Delete(Lookups.PreviouslyEncodingTempFile);
                    // Log occurred in catch
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

        public static void EncodeWithDolbyVision(EncodingJob job, string ffmpegDir, Logger logger, CancellationToken cancellationToken)
        {
            job.Status = EncodingJobStatus.ENCODING;

            if (ServerMethods.CheckForCancellation(job, logger, cancellationToken)) return;

            CancellationTokenSource encodingToken = new();
            Process videoEncodeProcess = null;
            Process audioSubEncodeProcess = null;
            Process mergeProcess = null;
            DolbyVisionEncodingCommandArguments arguments = job.EncodingCommandArguments as DolbyVisionEncodingCommandArguments;

            Stopwatch stopwatch = new();
            
            // ENCODING
            try
            {
                int count = 0;

                ProcessStartInfo videoEncodeStartInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/bin/bash" : "cmd",
                    Arguments = arguments.VideoEncodingCommandArguments,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                videoEncodeProcess = new()
                {
                    StartInfo = videoEncodeStartInfo,
                    EnableRaisingEvents = true
                };
                videoEncodeProcess.ErrorDataReceived += (sender, e) =>
                {
                    Process proc = sender as Process;
                    if (cancellationToken.IsCancellationRequested is true || encodingToken.Token.IsCancellationRequested is true)
                    {
                        proc.CancelErrorRead();
                        proc.Kill();
                        return;
                    }

                    job.ElapsedEncodingTime = stopwatch.Elapsed;

                    if (string.IsNullOrWhiteSpace(e.Data) is false)
                    {
                        HandleEncodingOutput(e.Data, job, ref count);
                    }
                }; 
                videoEncodeProcess.Exited += (sender, e) =>
                {
                    Process proc = sender as Process;
                    if (proc.ExitCode != 0)
                    {
                        encodingToken.Cancel();
                        File.Delete(job.EncodingInstructions.EncodedVideoFullPath);
                    }
                    else if (File.Exists(job.EncodingInstructions.EncodedVideoFullPath) is false)
                    {
                        encodingToken.Cancel();
                    }
                    else if (new FileInfo(job.EncodingInstructions.EncodedVideoFullPath).Length <= 0)
                    {
                        encodingToken.Cancel();
                        File.Delete(job.EncodingInstructions.EncodedVideoFullPath);
                    }
                };

                stopwatch.Start();
                videoEncodeProcess.Start();
                videoEncodeProcess.BeginErrorReadLine();

                ProcessStartInfo audioSubEncodeStartInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = Path.Combine(ffmpegDir, "ffmpeg"),
                    Arguments = arguments.AudioSubsEncodingCommandArguments,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                audioSubEncodeProcess = new()
                {
                    StartInfo = audioSubEncodeStartInfo,
                    EnableRaisingEvents = true
                };
                audioSubEncodeProcess.ErrorDataReceived += (sender, e) =>
                {
                    Process proc = sender as Process;
                    if (cancellationToken.IsCancellationRequested is true || encodingToken.Token.IsCancellationRequested)
                    {
                        proc.CancelErrorRead();
                        proc.Kill();
                        return;
                    };
                };
                audioSubEncodeProcess.Exited += (sender, e) =>
                {
                    Process proc = sender as Process;
                    if (proc.ExitCode != 0)
                    {
                        encodingToken.Cancel();
                        File.Delete(job.EncodingInstructions.EncodedAudioSubsFullPath);
                    }
                    else if (File.Exists(job.EncodingInstructions.EncodedAudioSubsFullPath) is false)
                    {
                        encodingToken.Cancel();
                    }
                    else if (new FileInfo(job.EncodingInstructions.EncodedAudioSubsFullPath).Length >= 0)
                    {
                        encodingToken.Cancel();
                        File.Delete(job.EncodingInstructions.EncodedAudioSubsFullPath);
                    }
                };
                audioSubEncodeProcess.Start();
                audioSubEncodeProcess.WaitForExit();
                audioSubEncodeProcess.Close();

                videoEncodeProcess.WaitForExit();
                stopwatch.Stop();
                job.ElapsedEncodingTime = stopwatch.Elapsed;
                videoEncodeProcess.Close();
                
            }
            catch (Exception ex)
            {
                string msg = $"Error encoding {job}.";
                logger.LogException(ex, msg);
                Debug.WriteLine($"{msg} : {ex.Message}");
                job.SetError(msg);
            }

            try
            {
                // CANCELLED
                if (cancellationToken.IsCancellationRequested is true)
                {
                    // Ensure these files are deleted (should've deleted on exit)
                    File.Delete(job.EncodingInstructions.EncodedVideoFullPath);
                    File.Delete(job.EncodingInstructions.EncodedAudioSubsFullPath);
                    job.ResetEncoding();
                    logger.LogError($"{job} was cancelled.");
                    return;
                }
                // Most likely, one of the processes failed
                else if (encodingToken.Token.IsCancellationRequested is true)
                {
                    // Ensure these files are deleted (should've deleted on exit)
                    File.Delete(job.EncodingInstructions.EncodedVideoFullPath);
                    File.Delete(job.EncodingInstructions.EncodedAudioSubsFullPath);
                    string msg = $"One of the encoding (video/audio-sub) processes errored for {job}";
                    logger.LogError(msg);
                    job.SetError(msg);
                    return;
                }
                // DIDN'T FINISH BUT DIDN'T RECEIVE ERROR
                else if (job.Error is false && job.EncodingProgress < 75)
                {
                    // Ensure these files are deleted (should've deleted on exit)
                    File.Delete(job.EncodingInstructions.EncodedVideoFullPath);
                    File.Delete(job.EncodingInstructions.EncodedAudioSubsFullPath);
                    job.SetError($"{job} encoding job ended prematurely.");
                    logger.LogError($"{job} encoding job ended prematurely.");
                    return;
                }
                // JOB ERRORED
                else if (job.Error is true)
                {
                    // Go ahead and clear out the temp file AND the encoded file (most likely didn't finish)
                    File.Delete(job.EncodingInstructions.EncodedVideoFullPath);
                    File.Delete(job.EncodingInstructions.EncodedAudioSubsFullPath);
                    // Log occurred in catch
                    return;
                }
            }
            catch (Exception ex)
            {
                // Most likely an exception from File.Delete
                string msg = $"Error cleaning up dolby vision encoding job.";
                logger.LogException(ex, msg);
                Debug.WriteLine($"{msg} : {ex.Message}");
                return;
                // Don't error the job for now
            }

            // MERGE
            try
            {
                ProcessStartInfo mergeStartInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = "mkvmerge",
                    Arguments = arguments.MergeCommandArguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };

                mergeProcess = new()
                {
                    StartInfo = mergeStartInfo
                };
                mergeProcess.OutputDataReceived += (sender, e) =>
                {
                    Process proc = sender as Process;
                    if (cancellationToken.IsCancellationRequested is true)
                    {
                        proc.CancelErrorRead();
                        proc.Kill();
                        return;
                    };
                };
                mergeProcess.Exited += (sender, e) =>
                {
                    Process proc = sender as Process;
                    if (proc.ExitCode != 0)
                    {
                        File.Delete(job.DestinationFullPath);
                        string msg = $"Merge process for {job.Name} ended unsuccessfully.";
                        job.SetError(msg);
                        logger.LogError(msg);
                    }
                };

                File.WriteAllText(Lookups.PreviouslyEncodingTempFile, job.DestinationFullPath);
                mergeProcess.Start();
                mergeProcess.BeginOutputReadLine();
                mergeProcess.WaitForExit();
            }
            catch (Exception ex)
            {
                string msg = $"Error merging {job}.";
                logger.LogException(ex, msg);
                Debug.WriteLine($"{msg} : {ex.Message}");
                job.SetError(msg);
            }

            try
            {
                // SUCCESS
                if (job.EncodingProgress >= 75 && job.Error is false && File.Exists(job.DestinationFullPath))
                {
                    job.CompleteEncoding(stopwatch.Elapsed);
                    File.Delete(Lookups.PreviouslyEncodingTempFile);
                    File.Delete(job.EncodingInstructions.EncodedVideoFullPath);
                    File.Delete(job.EncodingInstructions.EncodedAudioSubsFullPath);
                    if (job.EncodingInstructions.VideoStreamEncodingInstructions.HasDynamicHDR is true)
                    {
                        // Delete all possible HDRMetadata files
                        ((IDynamicHDRData)job.SourceStreamData.VideoStream.HDRData).MetadataFullPaths.Select(x => x.Value).ToList().ForEach(y => File.Delete(y));
                    }
                    logger.LogInfo($"Successfully encoded {job}. Estimated Time Elapsed: {stopwatch.Elapsed:hh\\:mm\\:ss}");
                }
                // CANCELLED
                else if (cancellationToken.IsCancellationRequested is true)
                {
                    // Ensure these files are deleted (should've deleted on exit)
                    File.Delete(job.DestinationFullPath);
                    job.ResetEncoding();
                    logger.LogError($"{job} was cancelled.");
                }
                // DIDN'T FINISH BUT DIDN'T RECEIVE ERROR
                else if (job.Error is false && job.EncodingProgress < 75)
                {
                    // Ensure these files are deleted (should've deleted on exit)
                    File.Delete(job.DestinationFullPath);
                    job.SetError($"{job} encoding job ended prematurely.");
                    logger.LogError($"{job} encoding job ended prematurely.");
                }
                // JOB ERRORED
                else if (job.Error is true)
                {
                    // Go ahead and clear out the temp file AND the encoded file (most likely didn't finish)
                    File.Delete(job.DestinationFullPath);
                    // Log occurred in catch
                }
                else if (File.Exists(job.DestinationFullPath) is false)
                {
                    string msg = $"Output file not created for {job}";
                    job.SetError(msg);
                    logger.LogError(msg);
                }
            }
            catch (Exception ex)
            {
                // Most likely an exception from File.Delete
                string msg = $"Error cleaning up dolby vision merging job.";
                logger.LogException(ex, msg);
                Debug.WriteLine($"{msg} : {ex.Message}");
                return;
                // Don't error the job for now
            }
        }

        private static void HandleEncodingOutput(string data, EncodingJob job, ref int count)
        {
            // Only check output every 50 events, don't need to do this frequently
            if (count >= 50)
            {
                if (data.Contains("time="))
                {
                    string line = data;
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
        }
    }
}
