using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
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
        /// <param name="logger"><see cref="ILogger"/></param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        public static void Encode(EncodingJob job, string ffmpegDir, ILogger logger, CancellationToken cancellationToken)
        {
            job.SetStatus(EncodingJobStatus.ENCODING);

            // Ensure everything is good for encoding
            PreEncodeVerification(job, logger);

            if (job.Error is true) return;

            Stopwatch stopwatch = new();
            Process encodingProcess = null;
            int? exitCode = null;

            CancellationTokenRegistration tokenRegistration = cancellationToken.Register(() =>
            {
                encodingProcess?.Kill(true);
            });

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

                using (encodingProcess = new())
                {
                    encodingProcess.StartInfo = startInfo;
                    encodingProcess.EnableRaisingEvents = true;
                    encodingProcess.ErrorDataReceived += (sender, e) =>
                    {
                        job.ElapsedEncodingTime = stopwatch.Elapsed;

                        if (count >= 50)
                        {
                            if (string.IsNullOrWhiteSpace(e.Data) is false)
                            {
                                job.UpdateEncodingProgress(HandleEncodingOutput(e.Data, job.SourceStreamData.DurationInSeconds));
                            }
                            count = 0;
                        }
                        else
                        {
                            count++;
                        }
                    };
                    encodingProcess.Exited += (sender, e) =>
                    {
                        if (sender is Process proc)
                        {
                            exitCode = proc.ExitCode;
                        }
                    };

                    File.WriteAllText(Lookups.PreviouslyEncodingTempFile, job.DestinationFullPath);
                    stopwatch.Start();
                    encodingProcess.Start();
                    encodingProcess.BeginErrorReadLine();
                    encodingProcess.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                job.SetError(logger.LogException(ex, $"Error encoding {job}.", details: new {job.Id, job.Name, ffmpegDir}));
            }

            stopwatch.Stop();
            tokenRegistration.Unregister();

            try
            {
                bool nonEmptyFileExists = File.Exists(job.DestinationFullPath) && new FileInfo(job.DestinationFullPath).Length > 0;
                // CANCELLED
                if (cancellationToken.IsCancellationRequested is true)
                {
                    // Go ahead and clear out the temp file AND the encoded file (most likely didn't finish)
                    DeleteFiles(job.DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    job.ResetEncoding();
                    logger.LogWarning($"Encoding of {job} was cancelled.");
                }
                // NON-ZERO EXIT CODE
                else if (exitCode is not null && exitCode != 0)
                {
                    DeleteFiles(job.DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    job.SetError(logger.LogError($"{job} encoding job failed. Exit Code: {exitCode}"));
                }
                // FILE NOT CREATED / EMPTY FILE
                else if (nonEmptyFileExists is false)
                {
                    DeleteFiles(job.DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    job.SetError(logger.LogError($"{job} either did not create an output or created an empty file"));
                }
                // DIDN'T FINISH BUT DIDN'T RECEIVE ERROR
                else if (job.Error is false && job.EncodingProgress < 95)
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
                        job.SourceStreamData.VideoStream.HDRData.DynamicMetadataFullPaths.Select(x => x.Value).ToList().ForEach(y => File.Delete(y));
                    }
                    logger.LogInfo($"Successfully encoded {job}. Estimated Time Elapsed: {HelperMethods.FormatEncodingTime(stopwatch.Elapsed)}");
                }
            }
            catch (Exception ex)
            {
                // Most likely an exception from File.Delete
                logger.LogException(ex, $"Error cleaning up encoding job.", details: new {job.Id, job.Name, job.EncodingProgress, job.Error});
                // Don't error the job for now
            }
        }

        /// <summary> Only for DolbyVision encodes. Calls ffmpeg/x265/mkvmerge to do encoding and merges; Handles output from ffmpeg </summary>
        /// <param name="job">The <see cref="EncodingJob"/> to be encoded.</param>
        /// <param name="ffmpegDir">The directory ffmpeg/ffprobe is located in.</param>
        /// <param name="logger"><see cref="ILogger"/></param>
        /// <param name="taskCancellationToken"><see cref="CancellationToken"/></param>
        public static void EncodeWithDolbyVision(EncodingJob job, string ffmpegDir, string mkvMergeFullPath, ILogger logger, CancellationToken taskCancellationToken)
        {
            job.SetStatus(EncodingJobStatus.ENCODING);

            // Ensure everything is good for encoding
            PreEncodeVerification(job, logger);

            if (job.Error is true) return;

            Process videoEncodeProcess = null;
            Process audioSubEncodeProcess = null;
            Process mergeProcess = null;
            int? videoEncodeExitCode = null;
            int? audioSubEncodeExitCode = null;
            DolbyVisionEncodingCommandArguments arguments = job.EncodingCommandArguments as DolbyVisionEncodingCommandArguments;

            CancellationTokenSource encodingTokenSource = new();
            CancellationToken encodingToken = encodingTokenSource.Token;

            CancellationTokenRegistration tokenRegistration = taskCancellationToken.Register(() =>
            {
                videoEncodeProcess?.Kill(true);
                audioSubEncodeProcess?.Kill(true);
            });

            CancellationTokenRegistration innerTokenRegistration = encodingToken.Register(() =>
            {
                videoEncodeProcess?.Kill(true);
                audioSubEncodeProcess?.Kill(true);
            });


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
                ProcessStartInfo audioSubEncodeStartInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = Path.Combine(ffmpegDir, "ffmpeg"),
                    Arguments = arguments.AudioSubsEncodingCommandArguments,
                    //RedirectStandardError = true,
                    UseShellExecute = false
                };

                using (videoEncodeProcess = new Process())
                using (audioSubEncodeProcess = new Process())
                {
                    // VIDEO
                    videoEncodeProcess.StartInfo = videoEncodeStartInfo;
                    videoEncodeProcess.EnableRaisingEvents = true;
                    videoEncodeProcess.ErrorDataReceived += (sender, e) =>
                    {
                        try
                        {
                            job.ElapsedEncodingTime = stopwatch.Elapsed;

                            if (count >= 50)
                            {
                                if (string.IsNullOrWhiteSpace(e.Data) is false)
                                {
                                    job.UpdateEncodingProgress(HandleDolbyVisionEncodingOutput(e.Data, job.SourceStreamData.NumberOfFrames, 0.9));
                                }
                                count = 0;
                            }
                            else
                            {
                                count++;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Just log for now
                            logger.LogException(ex, $"Exception occurred during data receive of video encoding process for {job}.", details: new { job.Id, job.Name });
                            return;
                        }
                    };
                    videoEncodeProcess.Exited += (sender, e) =>
                    {
                        if (sender is Process proc)
                        {
                            videoEncodeExitCode = proc.ExitCode;
                            if (videoEncodeExitCode != 0 ||
                                File.Exists(job.EncodingInstructions.EncodedVideoFullPath) is false ||
                                new FileInfo(job.EncodingInstructions.EncodedVideoFullPath).Length <= 0)
                            {
                                encodingTokenSource.Cancel();
                            }
                        }
                    };
                    // Start Video encode
                    if (videoEncodeProcess.Start() is false)
                    {
                        // If failed to start, error and end
                        job.SetError(logger.LogError($"Video encoding failed to start for {job}"));
                        return;
                    }

                    stopwatch.Start();
                    videoEncodeProcess.BeginErrorReadLine();

                    // AUDIO
                    audioSubEncodeProcess.StartInfo = audioSubEncodeStartInfo;
                    audioSubEncodeProcess.EnableRaisingEvents = true;
                    audioSubEncodeProcess.Exited += (sender, e) =>
                    {
                        if (sender is Process proc)
                        {
                            audioSubEncodeExitCode = proc?.ExitCode;
                            if (audioSubEncodeExitCode != 0 ||
                                File.Exists(job.EncodingInstructions.EncodedAudioSubsFullPath) is false ||
                                new FileInfo(job.EncodingInstructions.EncodedAudioSubsFullPath).Length <= 0)
                            {
                                encodingTokenSource.Cancel();
                            }
                        }

                    };

                    // Start audio/sub
                    if (audioSubEncodeProcess.Start() is false)
                    {
                        // If failed to start, error and end
                        job.SetError(logger.LogError($"Audio/Sub encoding failed to start for {job}"));
                        encodingTokenSource.Cancel();
                    }

                    File.WriteAllLines(Lookups.PreviouslyEncodingTempFile, new string[] { job.EncodingInstructions.EncodedVideoFullPath, job.EncodingInstructions.EncodedAudioSubsFullPath });

                    audioSubEncodeProcess.WaitForExit();
                    videoEncodeProcess.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                job.SetError(logger.LogException(ex, $"Error encoding {job}.", 
                    details: new {job.Id, job.Name, VideoEncodeProcess = videoEncodeProcess?.ProcessName, AudioSubEncodeProcess = audioSubEncodeProcess?.ProcessName}));
                videoEncodeProcess?.Kill(true);
                audioSubEncodeProcess?.Kill(true);
            }

            tokenRegistration.Unregister();
            innerTokenRegistration.Unregister();
            innerTokenRegistration.Dispose();

            // CHECKING AND CLEANUP OF ENCODING
            try
            {
                // CANCELLED
                if (taskCancellationToken.IsCancellationRequested is true)
                {
                    // Ensure these files are deleted (should've deleted on exit)
                    DeleteFiles(job.EncodingInstructions.EncodedVideoFullPath, job.EncodingInstructions.EncodedAudioSubsFullPath, Lookups.PreviouslyEncodingTempFile);
                    job.ResetEncoding();
                    logger.LogWarning($"{job} encoding was cancelled.");
                    return;
                }
                // Most likely, one of the processes failed
                else if (encodingToken.IsCancellationRequested is true)
                {
                    // Ensure these files are deleted (should've deleted on exit)
                    DeleteFiles(job.EncodingInstructions.EncodedVideoFullPath, job.EncodingInstructions.EncodedAudioSubsFullPath, Lookups.PreviouslyEncodingTempFile);
                    string[] messages = { $"One of the encoding (video/audio-sub) processes errored for {job}.",
                                     $"Video Encode Exit Code: {videoEncodeExitCode} | Audio/Sub Encode Exit Code: {audioSubEncodeExitCode}"};
                    job.SetError(logger.LogError(messages));
                    return;
                }
                // DIDN'T FINISH BUT DIDN'T RECEIVE ERROR
                else if (job.Error is false && job.EncodingProgress < 85)
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
                logger.LogException(ex, $"Error cleaning up dolby vision encoding job for {job}.", details: new { job.Id, job.Name, job.EncodingProgress, job.Error });
                return;
                // Don't error the job for now
            }

            // MERGE
            int? mergeExitCode = null;
            tokenRegistration = taskCancellationToken.Register(() =>
            {
                mergeProcess?.Kill(true);
            });

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

                using (mergeProcess = new Process())
                {
                    mergeProcess.StartInfo = mergeStartInfo;
                    mergeProcess.EnableRaisingEvents = true;
                    mergeProcess.OutputDataReceived += (sender, e) =>
                    {
                        try
                        {
                            job.ElapsedEncodingTime = stopwatch.Elapsed;
                        }

                        catch (Exception ex)
                        {
                            logger.LogException(ex, $"Error occurred during output data receive for mkvmerge of {job}.", details: new { job.Id, job.Name });
                            return;
                        }
                    };
                    mergeProcess.Exited += (sender, e) =>
                    {
                        if (sender is Process proc)
                        {
                            mergeExitCode = proc?.ExitCode;
                            if (mergeExitCode != 0)
                            {
                                job.SetError(logger.LogError($"Merge process for {job} ended unsuccessfully. ExitCode: {mergeExitCode}"));
                            }
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
                }
            }
            catch (Exception ex)
            {
                job.SetError(logger.LogException(ex, $"Error merging {job}.", details: new {job.Id, job.Name, mkvMergeFullPath, MergeProcess = mergeProcess?.ProcessName}));
                mergeProcess?.Kill(true);
            }

            try
            {
                bool nonEmptyFileExists = File.Exists(job.DestinationFullPath) && new FileInfo(job.DestinationFullPath).Length > 0;
                stopwatch.Stop();

                // CANCELLED
                if (taskCancellationToken.IsCancellationRequested is true)
                {
                    // Ensure these files are deleted
                    DeleteFiles(job.DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    job.ResetEncoding();
                    logger.LogWarning($"{job} encoding was cancelled.");
                }
                // SUCCESS
                else if (job.EncodingProgress >= 90 && job.Error is false && nonEmptyFileExists)
                {
                    job.CompleteEncoding(stopwatch.Elapsed);
                    DeleteFiles(Lookups.PreviouslyEncodingTempFile, job.EncodingInstructions.EncodedVideoFullPath, job.EncodingInstructions.EncodedAudioSubsFullPath);
                    if (job.EncodingInstructions.VideoStreamEncodingInstructions.HasDynamicHDR is true)
                    {
                        // Delete all possible HDRMetadata files
                        job.SourceStreamData.VideoStream.HDRData.DynamicMetadataFullPaths.Select(x => x.Value).ToList().ForEach(y => File.Delete(y));
                    }
                    logger.LogInfo($"Successfully encoded {job}. Estimated Time Elapsed: {HelperMethods.FormatEncodingTime(stopwatch.Elapsed)}");
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
                logger.LogException(ex, $"Error cleaning up dolby vision merging job for {job}.", details: new { job.Id, job.Name, job.EncodingProgress, job.Error });
                return;
                // Don't error the job for now
            }

            tokenRegistration.Unregister();
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

        private static void PreEncodeVerification(EncodingJob job, ILogger logger)
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
                job.SetError(logger.LogException(ex, $"Failed PreEncodeVerification for {job}.", details: new {job.Id, job.Name, job.SourceFullPath, job.DestinationFullPath}));
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
