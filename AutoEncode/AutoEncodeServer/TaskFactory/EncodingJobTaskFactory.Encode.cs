using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Interfaces;
using AutoEncodeUtilities.Logger;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace AutoEncodeServer.TaskFactory
{
    public static partial class EncodingJobTaskFactory
    {
        /// <summary> Calls ffmpeg to do encoding; Handles output from ffmpeg </summary>
        /// <param name="job">The <see cref="EncodingJob"/> to be encoded.</param>
        /// <param name="ffmpegDir">The directory ffmpeg/ffprobe is located in.</param>
        /// <param name="logger"><see cref="Logger"/></param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        public static void Encode(EncodingJob job, string ffmpegDir, Logger logger, CancellationToken cancellationToken)
        {
            job.SetStatus(EncodingJobStatus.ENCODING);

            // Ensure everything is good for encoding
            PreEncodeVerification(job, logger);

            if (job.Error is true) return;

            Stopwatch stopwatch = new();
            Process ffmpegProcess = null;
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

                ffmpegProcess = new()
                {
                    StartInfo = startInfo
                };
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

                    if (count >= 50)
                    {
                        if (string.IsNullOrWhiteSpace(e.Data) is false)
                        {
                            job.UpdateEncodingProgress(HandleEncodingOutput(e.Data, job.SourceStreamData.DurationInSeconds) ?? job.EncodingProgress);
                        }
                        count = 0;
                    }
                    else
                    {
                        count++;
                    }
                };

                File.WriteAllText(Lookups.PreviouslyEncodingTempFile, job.DestinationFullPath);
                stopwatch.Start();
                ffmpegProcess.Start();
                ffmpegProcess.BeginErrorReadLine();
                ffmpegProcess.WaitForExit();
            }
            catch (Exception ex)
            {
                job.SetError(logger.LogException(ex, $"Error encoding {job}."));
            }

            stopwatch.Stop();

            try
            {
                bool nonEmptyFileExists = File.Exists(job.DestinationFullPath) && new FileInfo(job.DestinationFullPath).Length > 0;
                // CANCELLED
                if (cancellationToken.IsCancellationRequested is true)
                {
                    // Go ahead and clear out the temp file AND the encoded file (most likely didn't finish)
                    DeleteFiles(job.DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    job.ResetEncoding();
                    logger.LogError($"{job} was cancelled.");
                }
                // NON-ZERO EXIT CODE / NULL PROCESS
                else if ((ffmpegProcess?.ExitCode ?? -1) != 0)
                {
                    DeleteFiles(job.DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    job.SetError(logger.LogError($"{job} encoding job failed. Exit Code: {(ffmpegProcess is null ? "NULL PROCESS" : ffmpegProcess.ExitCode)}"));
                }
                // FILE NOT CREATED / EMPTY FILE
                else if (nonEmptyFileExists is false)
                {
                    DeleteFiles(job.DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    job.SetError(logger.LogError($"{job} either did not create an output or created an empty file"));
                }
                // DIDN'T FINISH BUT DIDN'T RECEIVE ERROR
                else if (job.Error is false && job.EncodingProgress < 75)
                {
                    // Go ahead and clear out the temp file AND the encoded file (most likely didn't finish)
                    DeleteFiles(job.DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    job.SetError(logger.LogError($"{job} encoding job ended prematurely."));
                }
                // JOB ERRORED
                else if (job.Error is true)
                {
                    // Go ahead and clear out the temp file AND the encoded file (most likely didn't finish)
                    DeleteFiles(job.DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    // Log occurred in catch
                }
                // SUCCESS
                else if (job.EncodingProgress >= 75 && job.Error is false)
                {
                    job.CompleteEncoding(stopwatch.Elapsed);
                    DeleteFiles(Lookups.PreviouslyEncodingTempFile);
                    if (job.EncodingInstructions.VideoStreamEncodingInstructions.HasDynamicHDR is true)
                    {
                        // Delete all possible HDRMetadata files
                        ((IDynamicHDRData)job.SourceStreamData.VideoStream.HDRData).MetadataFullPaths.Select(x => x.Value).ToList().ForEach(y => File.Delete(y));
                    }
                    logger.LogInfo($"Successfully encoded {job}. Estimated Time Elapsed: {HelperMethods.FormatEncodingTime(stopwatch.Elapsed)}");
                }
            }
            catch (Exception ex)
            {
                // Most likely an exception from File.Delete
                logger.LogException(ex, $"Error cleaning up encoding job.");
                // Don't error the job for now
            }
        }

        /// <summary> Only for DolbyVision encodes. Calls ffmpeg/x265/mkvmerge to do encoding and merges; Handles output from ffmpeg </summary>
        /// <param name="job">The <see cref="EncodingJob"/> to be encoded.</param>
        /// <param name="ffmpegDir">The directory ffmpeg/ffprobe is located in.</param>
        /// <param name="logger"><see cref="Logger"/></param>
        /// <param name="taskCancellationToken"><see cref="CancellationToken"/></param>
        public static void EncodeWithDolbyVision(EncodingJob job, string ffmpegDir, string mkvMergeFullPath, Logger logger, CancellationToken taskCancellationToken)
        {
            job.SetStatus(EncodingJobStatus.ENCODING);

            // Ensure everything is good for encoding
            PreEncodeVerification(job, logger);

            if (job.Error is true) return;

            CancellationTokenSource encodingTokenSource = new();
            CancellationToken encodingToken = encodingTokenSource.Token;
            Process videoEncodeProcess = null;
            Process audioSubEncodeProcess = null;
            Process mergeProcess = null;
            DolbyVisionEncodingCommandArguments arguments = job.EncodingCommandArguments as DolbyVisionEncodingCommandArguments;

            Stopwatch stopwatch = new();

            // ENCODING
            try
            {
                int count = 0;

                // Video
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
                    try
                    {
                        taskCancellationToken.ThrowIfCancellationRequested();
                        encodingToken.ThrowIfCancellationRequested();

                        job.ElapsedEncodingTime = stopwatch.Elapsed;

                        if (count >= 50)
                        {
                            if (string.IsNullOrWhiteSpace(e.Data) is false)
                            {
                                job.UpdateEncodingProgress(HandleDolbyVisionEncodingOutput(e.Data, job.SourceStreamData.NumberOfFrames, 0.9) ?? job.EncodingProgress);
                            }
                            count = 0;
                        }
                        else
                        {
                            count++;
                        }

                    }
                    catch (OperationCanceledException)
                    {
                        proc.CancelErrorRead();
                        proc.Kill(true);
                        return;
                    }
                    catch (Exception ex)
                    {
                        // Just log for now
                        logger.LogException(ex, $"Exception occurred during data receive of video encoding process for {job}.");
                        return;
                    }
                };
                videoEncodeProcess.Exited += (sender, e) =>
                {
                    Process proc = sender as Process;
                    if (proc.ExitCode != 0 || 
                        File.Exists(job.EncodingInstructions.EncodedVideoFullPath) is false ||
                        new FileInfo(job.EncodingInstructions.EncodedVideoFullPath).Length <= 0)
                    {
                        encodingTokenSource.Cancel();
                    }
                };

                if (videoEncodeProcess.Start() is false)
                {
                    // If failed to start, error and end
                    job.SetError(logger.LogError($"Video encoding failed to start for {job}"));
                    return;
                }

                stopwatch.Start();
                videoEncodeProcess.BeginErrorReadLine();

                // Audio
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
                    try
                    {
                        taskCancellationToken.ThrowIfCancellationRequested();
                        encodingToken.ThrowIfCancellationRequested();
                    }
                    catch (OperationCanceledException)
                    {
                        proc.CancelErrorRead();
                        proc.Kill();
                        return;
                    }
                    catch (Exception ex)
                    {
                        logger.LogException(ex, $"Exception occurred during data receive of audio/sub encoding process for {job}.");
                        return;
                    }
                };
                audioSubEncodeProcess.Exited += (sender, e) =>
                {
                    Process proc = sender as Process;
                    if (proc.ExitCode != 0 ||
                        File.Exists(job.EncodingInstructions.EncodedAudioSubsFullPath) is false ||
                        new FileInfo(job.EncodingInstructions.EncodedAudioSubsFullPath).Length <= 0)
                    {
                        encodingTokenSource.Cancel();
                    }
                };

                if (audioSubEncodeProcess.Start() is false)
                {
                    // If failed to start, error and end
                    job.SetError(logger.LogError($"Audio/Sub encoding failed to start for {job}"));
                    encodingTokenSource.Cancel();
                }
                audioSubEncodeProcess.BeginErrorReadLine();

                File.WriteAllLines(Lookups.PreviouslyEncodingTempFile, new string[] { job.EncodingInstructions.EncodedVideoFullPath, job.EncodingInstructions.EncodedAudioSubsFullPath });

                audioSubEncodeProcess.WaitForExit();
                audioSubEncodeProcess.Close();

                videoEncodeProcess.WaitForExit();
                videoEncodeProcess.Close();
            }
            catch (Exception ex)
            {
                job.SetError(logger.LogException(ex, $"Error encoding {job}."));
                videoEncodeProcess.Kill(true);
                audioSubEncodeProcess.Kill();
            }

            // CHECKING AND CLEANUP OF ENCODING
            try
            {
                // CANCELLED
                if (taskCancellationToken.IsCancellationRequested is true)
                {
                    // Ensure these files are deleted (should've deleted on exit)
                    DeleteFiles(job.EncodingInstructions.EncodedVideoFullPath, job.EncodingInstructions.EncodedAudioSubsFullPath, Lookups.PreviouslyEncodingTempFile);
                    job.ResetEncoding();
                    logger.LogError($"{job} encoding was cancelled.");
                    return;
                }
                // Most likely, one of the processes failed
                else if (encodingToken.IsCancellationRequested is true)
                {
                    // Ensure these files are deleted (should've deleted on exit)
                    DeleteFiles(job.EncodingInstructions.EncodedVideoFullPath, job.EncodingInstructions.EncodedAudioSubsFullPath, Lookups.PreviouslyEncodingTempFile);
                    string[] messages = { $"One of the encoding (video/audio-sub) processes errored for {job}.",
                                     $"Video Encode Exit Code: {(videoEncodeProcess is null ? "NULL VIDEO PROCESS" : videoEncodeProcess.ExitCode)} | Audio/Sub Encode Exit Code: {(audioSubEncodeProcess is null ? "NULL AUDIO/SUB PROCESS" : audioSubEncodeProcess.ExitCode)}"};
                    job.SetError(logger.LogError(messages));
                    return;
                }
                // DIDN'T FINISH BUT DIDN'T RECEIVE ERROR
                else if (job.Error is false && job.EncodingProgress < 75)
                {
                    // Ensure these files are deleted
                    DeleteFiles(job.EncodingInstructions.EncodedVideoFullPath, job.EncodingInstructions.EncodedAudioSubsFullPath, Lookups.PreviouslyEncodingTempFile);
                    job.SetError(logger.LogError($"{job} encoding job ended prematurely."));
                    return;
                }
                // JOB ERRORED
                else if (job.Error is true)
                {
                    DeleteFiles(job.EncodingInstructions.EncodedVideoFullPath, job.EncodingInstructions.EncodedAudioSubsFullPath, Lookups.PreviouslyEncodingTempFile);
                    // Log occurred in catch
                    return;
                }
                // SUCCESS
                else
                {
                    job.UpdateEncodingProgress(90); // Hard set progress to 90%
                }
            }
            catch (Exception ex)
            {
                // Most likely an exception from File.Delete
                logger.LogException(ex, $"Error cleaning up dolby vision encoding job for {job}.");
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
                    FileName = string.IsNullOrWhiteSpace(mkvMergeFullPath) ? "mkvmerge" : mkvMergeFullPath,
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
                    try
                    {
                        job.ElapsedEncodingTime = stopwatch.Elapsed;
                        taskCancellationToken.ThrowIfCancellationRequested();
                    }
                    catch (OperationCanceledException)
                    {
                        proc.CancelErrorRead();
                        proc.Kill();
                        return;
                    }
                    catch (Exception ex)
                    {
                        logger.LogException(ex, $"Error occurred during output data receive for mkvmerge of {job}.");
                        return;
                    }
                };
                mergeProcess.Exited += (sender, e) =>
                {
                    Process proc = sender as Process;
                    if (proc.ExitCode != 0)
                    {
                        job.SetError(logger.LogError($"Merge process for {job} ended unsuccessfully. ExitCode: {proc.ExitCode}"));
                    }
                };

                if (mergeProcess.Start() is false)
                {
                    // If failed to start, error and end
                    job.SetError(logger.LogError($"Mkvmerge failed to start for {job}"));
                    // Delete previous encoding files
                    DeleteFiles(job.EncodingInstructions.EncodedVideoFullPath, job.EncodingInstructions.EncodedAudioSubsFullPath, Lookups.PreviouslyEncodingTempFile);
                    return;
                }

                File.AppendAllText(Lookups.PreviouslyEncodingTempFile, job.DestinationFullPath);
                mergeProcess.BeginOutputReadLine();
                mergeProcess.WaitForExit();
                mergeProcess.Close();
            }
            catch (Exception ex)
            {
                job.SetError(logger.LogException(ex, $"Error merging {job}."));
                mergeProcess.Kill();
            }

            try
            {
                bool nonEmptyFileExists = File.Exists(job.DestinationFullPath) && new FileInfo(job.DestinationFullPath).Length > 0;
                stopwatch.Stop();
                // SUCCESS
                if (job.EncodingProgress >= 90 && job.Error is false && nonEmptyFileExists)
                {
                    job.CompleteEncoding(stopwatch.Elapsed);
                    DeleteFiles(Lookups.PreviouslyEncodingTempFile, job.EncodingInstructions.EncodedVideoFullPath, job.EncodingInstructions.EncodedAudioSubsFullPath);
                    if (job.EncodingInstructions.VideoStreamEncodingInstructions.HasDynamicHDR is true)
                    {
                        // Delete all possible HDRMetadata files
                        ((IDynamicHDRData)job.SourceStreamData.VideoStream.HDRData).MetadataFullPaths.Select(x => x.Value).ToList().ForEach(y => File.Delete(y));
                    }
                    logger.LogInfo($"Successfully encoded {job}. Estimated Time Elapsed: {HelperMethods.FormatEncodingTime(stopwatch.Elapsed)}");
                }
                // CANCELLED
                else if (taskCancellationToken.IsCancellationRequested is true)
                {
                    // Ensure these files are deleted
                    DeleteFiles(job.DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    job.ResetEncoding();
                    logger.LogError($"{job} was cancelled.");
                }
                // DIDN'T FINISH BUT DIDN'T RECEIVE ERROR
                else if (job.Error is false && job.EncodingProgress < 90)
                {
                    // Ensure these files are deleted
                    DeleteFiles(job.DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    job.SetError(logger.LogError($"{job} encoding job ended prematurely."));
                }
                // JOB ERRORED
                else if (job.Error is true)
                {
                    // Go ahead and clear out the merged file (most likely didn't finish)
                    DeleteFiles(job.DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    // Log occurred in catch
                }
                else if (nonEmptyFileExists is false)
                {
                    DeleteFiles(job.DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    job.SetError(logger.LogError($"Output file not created for {job}"));
                }
            }
            catch (Exception ex)
            {
                // Most likely an exception from File.Delete
                logger.LogException(ex, $"Error cleaning up dolby vision merging job for {job}.");
                return;
                // Don't error the job for now
            }
        }

        /// <summary>Handles the output from ffmpeg encoding process </summary>
        /// <param name="data">Raw string output</param>
        /// <param name="sourceDurationInSeconds">Source file duration in seconds</param>
        /// <param name="adjustment">Adjusts encoding progress output if needed; 1 by default.</param>
        /// <returns>Encoding Progress</returns>
        private static int? HandleEncodingOutput(string data, int sourceDurationInSeconds, double adjustment = 1.0)
        {
            int? encodingProgress = null;
            if (data.Contains("time=") && sourceDurationInSeconds > 0)
            {
                string line = data;
                string time = line.Substring(line.IndexOf("time="), 13);
                int seconds = HelperMethods.ConvertTimestampToSeconds(time.Split('=')[1]);
                encodingProgress = (int)((((double)seconds / (double)sourceDurationInSeconds) * 100) * adjustment); // Update percent complete
                Debug.WriteLine(encodingProgress);
            }
            return encodingProgress;
        }

        /// <summary>Handles the output from ffmpeg/x265 dolby vision encoding process</summary>
        /// <param name="data">Raw string output</param>
        /// <param name="sourceNumFrames">Source file number of frames from video</param>
        /// <param name="adjustment">Adjusts encoding progress output if needed; 1 by default.</param>
        /// <returns>Encoding Progress</returns>
        private static int? HandleDolbyVisionEncodingOutput(string data, int sourceNumFrames, double adjustment = 1.0)
        {
            int? encodingProgress = null;
            if (data.Contains("frames:") && sourceNumFrames > 0)
            {
                string line = data;
                string framesString = line[..line.IndexOf("frames:")].Trim();
                if (int.TryParse(framesString, out int frames))
                {
                    encodingProgress = (int)((((double)frames / (double)sourceNumFrames) * 100) * adjustment);
                }
            }
            return encodingProgress;
        }

        private static void PreEncodeVerification(EncodingJob job, Logger logger)
        {
            try
            {
                // Verify source file is still here
                if (File.Exists(job.SourceFullPath) is false)
                {
                    job.SetError(logger.LogError($"Source file no longer found for {job}"));
                    return;
                }

                // Verify desination path exists (mainly for files in further subdirectories like extras, TV shows)
                // If it doesn't exist, create it
                string destinationDirectory = Path.GetDirectoryName(job.DestinationFullPath);
                if (Directory.Exists(destinationDirectory) is false)
                {
                    Directory.CreateDirectory(destinationDirectory);
                }
            }
            catch (Exception ex)
            {
                job.SetError(logger.LogException(ex, $"Failed PreEncodeVerification for {job}."));
                return;
            }
        }

        private static void DeleteFiles(params string[] files)
        {
            foreach (string file in files)
            {
                File.Delete(file);
            }
        }
    }
}
