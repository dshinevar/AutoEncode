using AutoEncodeServer.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AutoEncodeServer.EncodingJob
{
    public partial class EncodingJobManager : IEncodingJobManager
    {
        private const string EncodingThreadName = "Encoding Thread";

        /// <summary> Calls ffmpeg to do encoding; Handles output from ffmpeg </summary>
        /// <param name="job">The <see cref="EncodingJob"/> to be encoded.</param>
        /// <param name="ffmpegDir">The directory ffmpeg/ffprobe is located in.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        private void Encode(IEncodingJobModel job, string ffmpegDir, CancellationToken cancellationToken)
        {
            job.SetStatus(EncodingJobStatus.ENCODING);

            // Ensure everything is good for encoding
            PreEncodeVerification(job);

            if (job.HasError is true) return;

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
                        (byte? encodingProgress, int? estimatedSecondsRemaining, double? currentFps) progress = (null, null, null);

                        if (count >= 10)
                        {
                            if (string.IsNullOrWhiteSpace(e.Data) is false)
                            {
                                progress = HandleEncodingOutput(e.Data, job.SourceStreamData.NumberOfFrames);
                            }
                            count = 0;
                        }
                        else
                        {
                            count++;
                        }

                        job.UpdateEncodingProgress(progress.encodingProgress, progress.estimatedSecondsRemaining, progress.currentFps, stopwatch.Elapsed);
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
                job.SetError(Logger.LogException(ex, $"Error encoding {job}.", details: new { job.Id, job.Name, ffmpegDir }), ex);
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
                    //job.ResetEncoding();
                    Logger.LogWarning($"Encoding of {job} was cancelled.", EncodingThreadName);
                }
                // NON-ZERO EXIT CODE
                else if (exitCode is not null && exitCode != 0)
                {
                    DeleteFiles(job.DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    job.SetError(Logger.LogError($"{job} encoding job failed. Exit Code: {exitCode}", EncodingThreadName, new { exitCode }));
                }
                // FILE NOT CREATED / EMPTY FILE
                else if (nonEmptyFileExists is false)
                {
                    DeleteFiles(job.DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    job.SetError(Logger.LogError($"{job} either did not create an output or created an empty file", EncodingThreadName));
                }
                // DIDN'T FINISH BUT DIDN'T RECEIVE ERROR
                else if (job.HasError is false && job.EncodingProgress < 95)
                {
                    // Go ahead and clear out the temp file AND the encoded file (most likely didn't finish)
                    DeleteFiles(job.DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    job.SetError(Logger.LogError($"{job} encoding job ended prematurely.", EncodingThreadName, new { job.HasError, job.EncodingProgress }));
                }
                // JOB ERRORED
                else if (job.HasError is true)
                {
                    // Go ahead and clear out the temp file AND the encoded file (most likely didn't finish)
                    DeleteFiles(job.DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    // Log occurred in catch
                }
                // SUCCESS
                else if (job.EncodingProgress >= 75 && job.HasError is false)
                {
                    job.CompleteEncoding(stopwatch.Elapsed);
                    DeleteFiles(Lookups.PreviouslyEncodingTempFile);
                    if (job.EncodingInstructions.VideoStreamEncodingInstructions.HasDynamicHDR is true)
                    {
                        // Delete all possible HDRMetadata files
                        job.SourceStreamData.VideoStream.HDRData.DynamicMetadataFullPaths.Select(x => x.Value).ToList().ForEach(File.Delete);
                    }
                    Logger.LogInfo($"Successfully encoded {job}. Estimated Time Elapsed: {HelperMethods.FormatEncodingTime(stopwatch.Elapsed)}", EncodingThreadName);
                }
            }
            catch (Exception ex)
            {
                // Most likely an exception from File.Delete
                Logger.LogException(ex, $"Error cleaning up encoding job.", details: new { job.Id, job.Name, job.EncodingProgress, job.HasError });
                // Don't error the job for now
            }
        }

        /// <summary> Only for DolbyVision encodes. Calls ffmpeg/x265/mkvmerge to do encoding and merges; Handles output from ffmpeg </summary>
        /// <param name="job">The <see cref="EncodingJob"/> to be encoded.</param>
        /// <param name="ffmpegDir">The directory ffmpeg/ffprobe is located in.</param>
        /// <param name="mkvMergeFullPath">The full path to mkvmerge</param>
        /// <param name="taskCancellationToken"><see cref="CancellationToken"/></param>
        public void EncodeWithDolbyVision(IEncodingJobModel job, string ffmpegDir, string mkvMergeFullPath, CancellationToken taskCancellationToken)
        {
            job.SetStatus(EncodingJobStatus.ENCODING);

            // Ensure everything is good for encoding
            PreEncodeVerification(job);

            if (job.HasError is true) return;

            bool videoEncodeExited = false;
            bool audioSubEncodeExited = false;
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
                if (videoEncodeExited is false) videoEncodeProcess?.Kill(true);
                if (audioSubEncodeExited is false) audioSubEncodeProcess?.Kill(true);
            });

            CancellationTokenRegistration innerTokenRegistration = encodingToken.Register(() =>
            {
                if (videoEncodeExited is false) videoEncodeProcess?.Kill(true);
                if (audioSubEncodeExited is false) audioSubEncodeProcess?.Kill(true);
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
                            (byte? encodingProgress, int? estimatedSecondsRemaining, double? currentFps) progress = (null, null, null);

                            if (count >= 50)
                            {
                                if (string.IsNullOrWhiteSpace(e.Data) is false)
                                {
                                    progress = HandleDolbyVisionEncodingOutput(e.Data, job.SourceStreamData.NumberOfFrames, 0.9);
                                }
                                count = 0;
                            }
                            else
                            {
                                count++;
                            }

                            job.UpdateEncodingProgress(progress.encodingProgress, progress.estimatedSecondsRemaining, progress.currentFps, stopwatch.Elapsed);
                        }
                        catch (Exception ex)
                        {
                            // Just log for now
                            Logger.LogException(ex, $"Exception occurred during data receive of video encoding process for {job}.", details: new { job.Id, job.Name });
                            return;
                        }
                    };
                    videoEncodeProcess.Exited += (sender, e) =>
                    {
                        videoEncodeExited = true;
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
                        job.SetError(Logger.LogError($"Video encoding failed to start for {job}", EncodingThreadName));
                        return;
                    }

                    stopwatch.Start();
                    videoEncodeProcess.BeginErrorReadLine();

                    // AUDIO
                    audioSubEncodeProcess.StartInfo = audioSubEncodeStartInfo;
                    audioSubEncodeProcess.EnableRaisingEvents = true;
                    audioSubEncodeProcess.Exited += (sender, e) =>
                    {
                        audioSubEncodeExited = true;
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
                        job.SetError(Logger.LogError($"Audio/Sub encoding failed to start for {job}", EncodingThreadName));
                        encodingTokenSource.Cancel();
                    }

                    File.WriteAllLines(Lookups.PreviouslyEncodingTempFile, new string[] { job.EncodingInstructions.EncodedVideoFullPath, job.EncodingInstructions.EncodedAudioSubsFullPath });

                    audioSubEncodeProcess.WaitForExit();
                    videoEncodeProcess.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                job.SetError(Logger.LogException(ex, $"Error encoding {job}.",
                    details: new { job.Id, job.Name, VideoEncodeProcess = videoEncodeProcess?.ProcessName, AudioSubEncodeProcess = audioSubEncodeProcess?.ProcessName }), ex);
                if (videoEncodeExited is false) videoEncodeProcess?.Kill(true);
                if (audioSubEncodeExited is false) audioSubEncodeProcess?.Kill(true);
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
                    Logger.LogWarning($"{job} encoding was cancelled.", EncodingThreadName);
                    return;
                }
                // Most likely, one of the processes failed
                else if (encodingToken.IsCancellationRequested is true)
                {
                    // Ensure these files are deleted (should've deleted on exit)
                    DeleteFiles(job.EncodingInstructions.EncodedVideoFullPath, job.EncodingInstructions.EncodedAudioSubsFullPath, Lookups.PreviouslyEncodingTempFile);
                    string[] messages = [ $"One of the encoding (video/audio-sub) processes errored for {job}.",
                                     $"Video Encode Exit Code: {videoEncodeExitCode} | Audio/Sub Encode Exit Code: {audioSubEncodeExitCode}"];
                    job.SetError(Logger.LogError(messages, EncodingThreadName, new { videoEncodeExitCode, audioSubEncodeExitCode }));
                    return;
                }
                // DIDN'T FINISH BUT DIDN'T RECEIVE ERROR
                else if (job.HasError is false && job.EncodingProgress < 85)
                {
                    // Ensure these files are deleted
                    DeleteFiles(job.EncodingInstructions.EncodedVideoFullPath, job.EncodingInstructions.EncodedAudioSubsFullPath, Lookups.PreviouslyEncodingTempFile);
                    job.SetError(Logger.LogError($"{job} encoding job ended prematurely.", EncodingThreadName, new { job.HasError, job.EncodingProgress }));
                    return;
                }
                // JOB ERRORED
                else if (job.HasError is true)
                {
                    DeleteFiles(job.EncodingInstructions.EncodedVideoFullPath, job.EncodingInstructions.EncodedAudioSubsFullPath, Lookups.PreviouslyEncodingTempFile);
                    // Log occurred in catch
                    return;
                }
                // SUCCESS
                else
                {
                    job.UpdateEncodingProgress(90, null, null, null); // Hard set progress to 90%
                }
            }
            catch (Exception ex)
            {
                // Most likely an exception from File.Delete
                Logger.LogException(ex, $"Error cleaning up dolby vision encoding job for {job}.", details: new { job.Id, job.Name, job.EncodingProgress, job.HasError });
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
                            job.UpdateEncodingProgress(null, null, null, stopwatch.Elapsed);
                        }

                        catch (Exception ex)
                        {
                            Logger.LogException(ex, $"Error occurred during output data receive for mkvmerge of {job}.", details: new { job.Id, job.Name });
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
                                job.SetError(Logger.LogError($"Merge process for {job} ended unsuccessfully. ExitCode: {mergeExitCode}", EncodingThreadName, new { mergeExitCode }));
                            }
                        }
                    };

                    if (mergeProcess.Start() is false)
                    {
                        // If failed to start, error and end
                        job.SetError(Logger.LogError($"Mkvmerge failed to start for {job}", EncodingThreadName));
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
                job.SetError(Logger.LogException(ex, $"Error merging {job}.", details: new { job.Id, job.Name, mkvMergeFullPath, MergeProcess = mergeProcess?.ProcessName }), ex);
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
                    //job.ResetEncoding();
                    Logger.LogWarning($"{job} encoding was cancelled.", EncodingThreadName);
                }
                // SUCCESS
                else if (job.EncodingProgress >= 90 && job.HasError is false && nonEmptyFileExists)
                {
                    job.CompleteEncoding(stopwatch.Elapsed);
                    DeleteFiles(Lookups.PreviouslyEncodingTempFile, job.EncodingInstructions.EncodedVideoFullPath, job.EncodingInstructions.EncodedAudioSubsFullPath);
                    if (job.EncodingInstructions.VideoStreamEncodingInstructions.HasDynamicHDR is true)
                    {
                        // Delete all possible HDRMetadata files
                        job.SourceStreamData.VideoStream.HDRData.DynamicMetadataFullPaths.Select(x => x.Value).ToList().ForEach(y => File.Delete(y));
                    }
                    Logger.LogInfo($"Successfully encoded {job}. Estimated Time Elapsed: {HelperMethods.FormatEncodingTime(stopwatch.Elapsed)}", EncodingThreadName);
                }
                // DIDN'T FINISH BUT DIDN'T RECEIVE ERROR
                else if (job.HasError is false && job.EncodingProgress < 90)
                {
                    // Ensure these files are deleted
                    DeleteFiles(job.DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    job.SetError(Logger.LogError($"{job} encoding job ended prematurely.", EncodingThreadName, new { job.HasError, job.EncodingProgress }));
                }
                // JOB ERRORED
                else if (job.HasError is true)
                {
                    // Go ahead and clear out the merged file (most likely didn't finish)
                    DeleteFiles(job.DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    // Log occurred in catch
                }
                else if (nonEmptyFileExists is false)
                {
                    DeleteFiles(job.DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    job.SetError(Logger.LogError($"Output file not created for {job}", EncodingThreadName));
                }
            }
            catch (Exception ex)
            {
                // Most likely an exception from File.Delete
                Logger.LogException(ex, $"Error cleaning up dolby vision merging job for {job}.", details: new { job.Id, job.Name, job.EncodingProgress, job.HasError });
                return;
                // Don't error the job for now
            }

            tokenRegistration.Unregister();
        }

        /// <summary>Handles the output from ffmpeg encoding process </summary>
        /// <param name="data">Raw string output</param>
        /// <param name="numberOfFrames">Number of frames source file contains.</param>
        /// <param name="adjustment">Adjusts encoding progress output if needed; 1 by default.</param>
        /// <returns>Encoding Progress</returns>
        private static (byte? encodingProgress, int? estimatedSecondsRemaining, double? currentFps) HandleEncodingOutput(string data, int numberOfFrames, double adjustment = 1.0)
        {
            byte? encodingProgress = null;
            int? estimatedSecondsRemaining = null;
            double? currentFps = null;

            int? framesRemaining = null;
            if (data.Contains("frame=") && numberOfFrames > 0)
            {
                string frameString = data.Substring(data.IndexOf("frame="), 11);
                if (int.TryParse(frameString.Split('=')[1].Trim(), out int currentFrame))
                {
                    encodingProgress = (byte)((double)currentFrame / (double)numberOfFrames * 100 * adjustment);
                    framesRemaining = numberOfFrames - currentFrame;
                }
            }

            if (framesRemaining is not null && data.Contains("fps=") && numberOfFrames > 0)
            {
                string fpsString = data.Substring(data.IndexOf("fps="), 7);
                if (double.TryParse(fpsString.Split("=")[1].Trim(), out double fps) is true)
                {
                    currentFps = fps;
                    estimatedSecondsRemaining = (int)(framesRemaining / currentFps);
                }          
            }
            return (encodingProgress, estimatedSecondsRemaining, currentFps);
        }

        /// <summary>Handles the output from ffmpeg/x265 dolby vision encoding process</summary>
        /// <param name="data">Raw string output</param>
        /// <param name="sourceNumFrames">Source file number of frames from video</param>
        /// <param name="adjustment">Adjusts encoding progress output if needed; 1 by default.</param>
        /// <returns>Encoding Progress</returns>
        private static (byte? encodingProgress, int? estimatedSecondsRemaining, double? currentFps) HandleDolbyVisionEncodingOutput(string data, int numberOfFrames, double adjustment = 1.0)
        {
            byte? encodingProgress = null;
            int? estimatedSecondsRemaining = null;
            double? currentFps = null;
            int? framesRemaining = null;

            if (data.Contains("frames:") && numberOfFrames > 0)
            {
                string framesString = data[..data.IndexOf("frames:")].Trim();
                if (int.TryParse(framesString, out int currentFrame) is true)
                {
                    encodingProgress = (byte)((((double)currentFrame / (double)numberOfFrames) * 100) * adjustment);
                    framesRemaining = numberOfFrames - currentFrame;
                }
            }

            if (framesRemaining is not null && data.Contains("fps") && numberOfFrames > 0)
            {
                int length = data.IndexOf("fps") - (data.IndexOf("frames:") + 7);
                string fpsString = data.Substring(data.IndexOf("frames:") + 7, length).Trim();
                if (double.TryParse(fpsString, out double fps) is true)
                {
                    currentFps = fps;
                    estimatedSecondsRemaining = (int)(framesRemaining / currentFps);
                }
            }

            return (encodingProgress, estimatedSecondsRemaining, currentFps);
        }

        private void PreEncodeVerification(IEncodingJobModel job)
        {
            try
            {
                // Verify source file is still here
                if (File.Exists(job.SourceFullPath) is false)
                {
                    job.SetError(Logger.LogError($"Source file no longer found for {job}", EncodingThreadName, new { job.SourceFullPath }));
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
                job.SetError(Logger.LogException(ex, $"Failed PreEncodeVerification for {job}.", details: new { job.Id, job.Name, job.SourceFullPath, job.DestinationFullPath }), ex);
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
