using AutoEncodeServer.Models.Interfaces;
using AutoEncodeServer.Utilities;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Base;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace AutoEncodeServer.Models;

public partial class EncodingJobModel :
    ModelBase,
    IEncodingJobModel
{
    public void Encode(CancellationTokenSource cancellationTokenSource)
    {
        TaskCancellationTokenSource = cancellationTokenSource;
        Status = EncodingJobStatus.ENCODING;

        CancellationToken cancellationToken = cancellationTokenSource.Token;

        HelperMethods.DebugLog($"ENCODE STARTED: {this}", nameof(EncodingJobModel));

        // Ensure everything is good for encoding
        try
        {
            // Verify source file is still here
            if (File.Exists(SourceFullPath) is false)
            {
                string msg = $"Source file no longer found for {this}";
                SetError(msg);
                Logger.LogError(msg, nameof(EncodingJobModel), new { SourceFullPath });
                return;
            }

            // Verify desination path exists (mainly for files in further subdirectories like extras, TV shows)
            // If it doesn't exist, create it
            string destinationDirectory = Path.GetDirectoryName(DestinationFullPath);
            if (Directory.Exists(destinationDirectory) is false)
            {
                Directory.CreateDirectory(destinationDirectory);
            }
        }
        catch (Exception ex)
        {
            string msg = $"Failed PreEncodeVerification for {this}.";
            SetError(ex, msg);
            Logger.LogException(ex, msg, nameof(EncodingJobModel), new { Id, Name, SourceFullPath, DestinationFullPath });
            return;
        }


        // Do the encode
        if (State.DolbyVision.Enabled is true && EncodingInstructions.VideoStreamEncodingInstructions.HasDolbyVision is true)
        {
            InternalEncodeWithDolbyVision();
        }
        else
        {
            InternalEncode();
        }


        void InternalEncode()
        {
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
                    FileName = Path.Combine(State.Ffmpeg.FfmpegDirectory, "ffmpeg"),
                    Arguments = EncodingCommandArguments.CommandArguments[0],
                    UseShellExecute = false,
                    RedirectStandardError = true
                };

                bool handle = false;

                using (encodingProcess = new())
                {
                    encodingProcess.StartInfo = startInfo;
                    encodingProcess.EnableRaisingEvents = true;
                    encodingProcess.ErrorDataReceived += (sender, e) =>
                    {
                        (byte? encodingProgress, int? estimatedSecondsRemaining, double? currentFps) progress = (null, null, null);

                        if (handle is true)
                        {
                            if (string.IsNullOrWhiteSpace(e.Data) is false)
                            {
                                progress = HandleEncodingOutput(e.Data, SourceStreamData.VideoStream.IsInterlaced);
                            }
                        }

                        handle |= !handle;

                        UpdateEncodingProgress(progress.encodingProgress, progress.estimatedSecondsRemaining, progress.currentFps, stopwatch.Elapsed);
                    };
                    encodingProcess.Exited += (sender, e) =>
                    {
                        if (sender is Process proc)
                        {
                            exitCode = proc.ExitCode;
                        }
                    };

                    File.WriteAllText(Lookups.PreviouslyEncodingTempFile, DestinationFullPath);
                    stopwatch.Start();
                    encodingProcess.Start();
                    encodingProcess.BeginErrorReadLine();
                    encodingProcess.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                string msg = $"Error encoding {this}.";
                SetError(ex, msg);
                Logger.LogException(ex, msg, nameof(EncodingJobModel), details: new { Id, Name, State.Ffmpeg.FfmpegDirectory });
            }

            stopwatch.Stop();
            tokenRegistration.Unregister();

            try
            {
                bool nonEmptyFileExists = File.Exists(DestinationFullPath) && new FileInfo(DestinationFullPath).Length > 0;
                // CANCELLED
                if (cancellationToken.IsCancellationRequested is true)
                {
                    // Go ahead and clear out the temp file AND the encoded file (most likely didn't finish)
                    HelperMethods.DeleteFiles(DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    Logger.LogWarning($"Encoding of {this} was cancelled.", nameof(EncodingJobModel));
                }
                // NON-ZERO EXIT CODE
                else if (exitCode is not null && exitCode != 0)
                {
                    HelperMethods.DeleteFiles(DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    string msg = $"{this} encoding job failed. Exit Code: {exitCode}";
                    SetError(msg);
                    Logger.LogError(msg, nameof(EncodingJobModel), new { exitCode });
                }
                // FILE NOT CREATED / EMPTY FILE
                else if (nonEmptyFileExists is false)
                {
                    HelperMethods.DeleteFiles(DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    string msg = $"{this} either did not create an output or created an empty file";
                    SetError(msg);
                    Logger.LogError(msg, nameof(EncodingJobModel));
                }
                // JOB ERRORED
                else if (HasError is true)
                {
                    // Go ahead and clear out the temp file AND the encoded file (most likely didn't finish)
                    HelperMethods.DeleteFiles(DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    // Log occurred in catch
                }
                // SUCCESS
                else
                {
                    List<string> log = [];
                    log.Add($"Successfully encoded {this}. Estimated Time Elapsed: {HelperMethods.FormatEncodingTime(stopwatch.Elapsed)}");
                    if (EncodingProgress < 90)
                    {
                        log.Add("NOTE: Encoding completed before the calculated encoding progress reached 90%.");
                        log.Add("No errors appear to have occurred but output file should be checked to ensure completeness.");
                    }

                    CompleteEncoding(stopwatch.Elapsed);
                    HelperMethods.DeleteFiles(Lookups.PreviouslyEncodingTempFile);
                    if (EncodingInstructions.VideoStreamEncodingInstructions.HasDynamicHDR is true)
                    {
                        // Delete all possible HDRMetadata files
                        SourceStreamData.VideoStream.HDRData.DynamicMetadataFullPaths.Select(x => x.Value).ToList().ForEach(File.Delete);
                    }

                    Logger.LogInfo(log, nameof(EncodingJobModel));
                }
            }
            catch (Exception ex)
            {
                // Most likely an exception from File.Delete
                Logger.LogException(ex, $"Error cleaning up encoding job.", details: new { Id, Name, EncodingProgress, HasError });
                // Don't error the job for now
            }
        }

        void InternalEncodeWithDolbyVision()
        {
            bool videoEncodeExited = false;
            bool audioSubEncodeExited = false;
            Process videoEncodeProcess = null;
            Process audioSubEncodeProcess = null;
            Process mergeProcess = null;
            int? videoEncodeExitCode = null;
            int? audioSubEncodeExitCode = null;

            CancellationToken taskCancellationToken = cancellationTokenSource.Token;    // Task-level token

            CancellationTokenSource encodingTokenSource = new();    // Token used specifically for encoding stage
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
                bool handle = false;

                ProcessStartInfo videoEncodeStartInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = State.IsLinuxEnvironment ? "/bin/bash" : "cmd",
                    Arguments = EncodingCommandArguments.CommandArguments[0],
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                ProcessStartInfo audioSubEncodeStartInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = Path.Combine(State.Ffmpeg.FfmpegDirectory, "ffmpeg"),
                    Arguments = EncodingCommandArguments.CommandArguments[1],
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

                            if (handle is true)
                            {
                                if (string.IsNullOrWhiteSpace(e.Data) is false)
                                {
                                    progress = HandleDolbyVisionEncodingOutput(e.Data, 0.9);
                                }
                            }

                            handle |= !handle;

                            UpdateEncodingProgress(progress.encodingProgress, progress.estimatedSecondsRemaining, progress.currentFps, stopwatch.Elapsed);
                        }
                        catch (Exception ex)
                        {
                            // Just log for now
                            Logger.LogException(ex, $"Exception occurred during data receive of video encoding process for {this}.", nameof(EncodingJobModel), details: new { Id, Name });
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
                                File.Exists(EncodingInstructions.EncodedVideoFullPath) is false ||
                                new FileInfo(EncodingInstructions.EncodedVideoFullPath).Length <= 0)
                            {
                                encodingTokenSource.Cancel();
                            }
                        }
                    };
                    // Start Video encode
                    if (videoEncodeProcess.Start() is false)
                    {
                        // If failed to start, error and end
                        string msg = $"Video encoding failed to start for {this}";
                        SetError(msg);
                        Logger.LogError(msg, nameof(EncodingJobModel));
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
                                File.Exists(EncodingInstructions.EncodedAudioSubsFullPath) is false ||
                                new FileInfo(EncodingInstructions.EncodedAudioSubsFullPath).Length <= 0)
                            {
                                encodingTokenSource.Cancel();
                            }
                        }

                    };

                    // Start audio/sub
                    if (audioSubEncodeProcess.Start() is false)
                    {
                        // If failed to start, error and end
                        string msg = $"Audio/Sub encoding failed to start for {this}";
                        SetError(msg);
                        Logger.LogError(msg, nameof(EncodingJobModel));
                        encodingTokenSource.Cancel();
                    }

                    File.WriteAllLines(Lookups.PreviouslyEncodingTempFile, [EncodingInstructions.EncodedVideoFullPath, EncodingInstructions.EncodedAudioSubsFullPath]);

                    audioSubEncodeProcess.WaitForExit();
                    videoEncodeProcess.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                string msg = $"Error encoding {this}.";
                SetError(ex, msg);
                Logger.LogException(ex, msg, nameof(EncodingJobModel),
                    new { Id, Name, VideoEncodeProcess = videoEncodeProcess?.ProcessName, AudioSubEncodeProcess = audioSubEncodeProcess?.ProcessName });
                if (videoEncodeExited is false)
                    videoEncodeProcess?.Kill(true);
                if (audioSubEncodeExited is false)
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
                    HelperMethods.DeleteFiles(EncodingInstructions.EncodedVideoFullPath, EncodingInstructions.EncodedAudioSubsFullPath, Lookups.PreviouslyEncodingTempFile);
                    Logger.LogWarning($"{this} encoding was cancelled.", nameof(EncodingJobModel));
                    return;
                }
                // Most likely, one of the processes failed
                else if (encodingToken.IsCancellationRequested is true)
                {
                    // Ensure these files are deleted (should've deleted on exit)
                    HelperMethods.DeleteFiles(EncodingInstructions.EncodedVideoFullPath, EncodingInstructions.EncodedAudioSubsFullPath, Lookups.PreviouslyEncodingTempFile);
                    string[] messages = [ $"One of the encoding (video/audio-sub) processes errored for {this}.",
                                 $"Video Encode Exit Code: {videoEncodeExitCode} | Audio/Sub Encode Exit Code: {audioSubEncodeExitCode}"];
                    SetError($"One of the encoding (video/audio-sub) processes errored for {this}.");
                    Logger.LogError(messages, nameof(EncodingJobModel), new { videoEncodeExitCode, audioSubEncodeExitCode });
                    return;
                }
                // JOB ERRORED
                else if (HasError is true)
                {
                    HelperMethods.DeleteFiles(EncodingInstructions.EncodedVideoFullPath, EncodingInstructions.EncodedAudioSubsFullPath, Lookups.PreviouslyEncodingTempFile);
                    // Log occurred in catch
                    return;
                }
                // SUCCESS
                else
                {
                    UpdateEncodingProgress(90, null, null, null); // Hard set progress to 90%
                }
            }
            catch (Exception ex)
            {
                // Most likely an exception from File.Delete
                Logger.LogException(ex, $"Error cleaning up dolby vision encoding job for {this}.", nameof(EncodingJobModel), new { Id, Name, EncodingProgress, HasError });
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
                    FileName = string.IsNullOrWhiteSpace(State.DolbyVision.MkvMergeFullPath) ? "mkvmerge" : State.DolbyVision.MkvMergeFullPath,
                    Arguments = EncodingCommandArguments.CommandArguments[2],
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
                            UpdateEncodingProgress(null, null, null, stopwatch.Elapsed);
                        }

                        catch (Exception ex)
                        {
                            Logger.LogException(ex, $"Error occurred during output data receive for mkvmerge of {this}.", nameof(EncodingJobModel), new { Id, Name });
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
                                string msg = $"Merge process for {this} ended unsuccessfully. ExitCode: {mergeExitCode}";
                                SetError(msg);
                                Logger.LogError(msg, nameof(EncodingJobModel), new { mergeExitCode });
                            }
                        }
                    };

                    if (mergeProcess.Start() is false)
                    {
                        // If failed to start, error and end
                        string msg = $"Mkvmerge failed to start for {this}";
                        SetError(msg);
                        Logger.LogError(msg, nameof(EncodingJobModel));
                        // Delete previous encoding files
                        HelperMethods.DeleteFiles(EncodingInstructions.EncodedVideoFullPath, EncodingInstructions.EncodedAudioSubsFullPath, Lookups.PreviouslyEncodingTempFile);
                        return;
                    }

                    File.AppendAllText(Lookups.PreviouslyEncodingTempFile, DestinationFullPath);
                    mergeProcess.BeginOutputReadLine();
                    mergeProcess.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                string msg = $"Error merging {this}.";
                SetError(ex, msg);
                Logger.LogException(ex, msg, nameof(EncodingJobModel), new { Id, Name, State.DolbyVision.MkvMergeFullPath, MergeProcess = mergeProcess?.ProcessName });
                mergeProcess?.Kill(true);
            }

            try
            {
                bool nonEmptyFileExists = File.Exists(DestinationFullPath) && new FileInfo(DestinationFullPath).Length > 0;
                stopwatch.Stop();

                // CANCELLED
                if (taskCancellationToken.IsCancellationRequested is true)
                {
                    // Ensure these files are deleted
                    HelperMethods.DeleteFiles(DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    //job.ResetEncoding();
                    Logger.LogWarning($"{this} encoding was cancelled.", nameof(EncodingJobModel));
                }
                // JOB ERRORED
                else if (HasError is true)
                {
                    // Go ahead and clear out the merged file (most likely didn't finish)
                    HelperMethods.DeleteFiles(DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    // Log occurred in catch
                }
                // EMPTY FILE
                else if (nonEmptyFileExists is false)
                {
                    HelperMethods.DeleteFiles(DestinationFullPath, Lookups.PreviouslyEncodingTempFile);
                    string msg = $"Output file not created for {this}";
                    SetError(msg);
                    Logger.LogError(msg, nameof(EncodingJobModel));
                }
                // SUCCESS
                else
                {
                    CompleteEncoding(stopwatch.Elapsed);
                    HelperMethods.DeleteFiles(Lookups.PreviouslyEncodingTempFile, EncodingInstructions.EncodedVideoFullPath, EncodingInstructions.EncodedAudioSubsFullPath);
                    if (EncodingInstructions.VideoStreamEncodingInstructions.HasDynamicHDR is true)
                    {
                        // Delete all possible HDRMetadata files
                        SourceStreamData.VideoStream.HDRData.DynamicMetadataFullPaths.Select(x => x.Value).ToList().ForEach(File.Delete);
                    }
                    Logger.LogInfo($"Successfully encoded {this}. Estimated Time Elapsed: {HelperMethods.FormatEncodingTime(stopwatch.Elapsed)}", nameof(EncodingJobModel));
                }
            }
            catch (Exception ex)
            {
                // Most likely an exception from File.Delete
                Logger.LogException(ex, $"Error cleaning up dolby vision merging job for {this}.", nameof(EncodingJobModel), details: new { Id, Name, EncodingProgress, HasError });
                return;
                // Don't error the job for now
            }

            tokenRegistration.Unregister();
        }

        (byte? encodingProgress, int? estimatedSecondsRemaining, double? currentFps) HandleEncodingOutput(string data, bool isInterlaced)
        {
            int numberOfFrames = isInterlaced ? SourceStreamData.NumberOfFrames * 2 : SourceStreamData.NumberOfFrames;

            byte? encodingProgress = null;
            int? estimatedSecondsRemaining = null;
            double? currentFps = null;

            int? framesRemaining = null;
            if (data.Contains("frame=") && numberOfFrames > 0)
            {
                int length = data.IndexOf("fps=") - data.IndexOf("frame=");
                string frameString = data.Substring(data.IndexOf("frame="), length);
                if (int.TryParse(frameString.Split('=')[1].Trim(), out int currentFrame))
                {
                    encodingProgress = (byte)((double)currentFrame / (double)numberOfFrames * 100);
                    framesRemaining = numberOfFrames - currentFrame;
                }
            }

            if (framesRemaining is not null && data.Contains("fps=") && numberOfFrames > 0)
            {
                int length = data.IndexOf("q=") - data.IndexOf("fps=");
                string fpsString = data.Substring(data.IndexOf("fps="), length);
                if (double.TryParse(fpsString.Split("=")[1].Trim(), out double fps) is true)
                {
                    currentFps = fps;
                    estimatedSecondsRemaining = (int)(framesRemaining / currentFps);
                }
            }
            return (encodingProgress, estimatedSecondsRemaining, currentFps);
        }

        (byte? encodingProgress, int? estimatedSecondsRemaining, double? currentFps) HandleDolbyVisionEncodingOutput(string data, double adjustment = 1.0)
        {
            int numberOfFrames = SourceStreamData.NumberOfFrames;

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
    }
}
