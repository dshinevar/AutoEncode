﻿using AutoEncodeServer.Models.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Base;
using AutoEncodeUtilities.Enums;
using System;
using System.IO;
using System.Threading;

namespace AutoEncodeServer.Models;

// POST-PROCESS
public partial class EncodingJobModel :
    ModelBase,
    IEncodingJobModel
{
    public void PostProcess(CancellationTokenSource cancellationTokenSource)
    {
        // Double-check to ensure we don't post-process a job that shouldn't be
        if (PostProcessingFlags.Equals(PostProcessingFlags.None))
            return;

        TaskCancellationTokenSource = cancellationTokenSource;
        Status = EncodingJobStatus.POST_PROCESSING;

        CancellationToken cancellationToken = cancellationTokenSource.Token;

        HelperMethods.DebugLog($"POSTPROCESS STARTED: {this}", nameof(EncodingJobModel));

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // COPY FILES
            if (PostProcessingFlags.HasFlag(PostProcessingFlags.Copy))
            {
                try
                {
                    foreach (string path in PostProcessingSettings.CopyFilePaths)
                    {
                        string copyDestinationDirectory = Path.GetDirectoryName(path);
                        if (Directory.Exists(copyDestinationDirectory) is false)
                        {
                            Directory.CreateDirectory(copyDestinationDirectory);
                        }

                        File.Copy(DestinationFullPath, path, true);
                    }
                }
                catch (Exception ex)
                {
                    SetError(Logger.LogException(ex, $"Error copying output file to other locations for {this}",
                       nameof(EncodingJobModel), new { Id, Name, PostProcessingSettings.CopyFilePaths, DestinationFullPath }), ex);
                    return;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            // DELETE SOURCE FILE
            if (PostProcessingFlags.HasFlag(PostProcessingFlags.DeleteSourceFile))
            {
                try
                {
                    File.Delete(SourceFullPath);
                }
                catch (Exception ex)
                {
                    SetError(Logger.LogException(ex, $"Error deleting source file for {this}", nameof(EncodingJobModel), new { Id, Name, SourceFullPath }), ex);
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning($"Post-Process was cancelled for {this}", nameof(EncodingJobModel));
            return;
        }
        catch (Exception ex)
        {
            SetError(Logger.LogException(ex, $"Error post-processing {this}", nameof(EncodingJobModel), new { Id, Name, Status }), ex);
            return;
        }

        CompletePostProcessing();
        Logger.LogInfo($"Successfully post-processed {this} encoding job.", nameof(EncodingJobModel));
    }
}
