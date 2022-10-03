using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegUtilities.Enums;
using AutomatedFFmpegUtilities.Logger;
using System;
using System.IO;
using System.Threading;

namespace AutomatedFFmpegServer.TaskFactory
{
    public static partial class EncodingJobTaskFactory
    {
        /// <summary> Runs post-processing tasks marked for the encoding job. </summary>
        /// <param name="job">The <see cref="EncodingJob"/> to be post-processed.</param>
        /// <param name="logger"><see cref="Logger"/></param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        public static void PostProcess(EncodingJob job, Logger logger, CancellationToken cancellationToken)
        {
            // Double-check to ensure we don't post-process a job that shouldn't be
            if (job.PostProcessingFlags.Equals(PostProcessingFlags.None)) return;

            job.SetStatus(EncodingJobStatus.POST_PROCESSING);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // COPY FILES
                if (job.PostProcessingFlags.HasFlag(PostProcessingFlags.Copy))
                {
                    try
                    {
                        foreach (string path in job.PostProcessingSettings.CopyFilePaths)
                        {
                            File.Copy(job.DestinationFullPath, Path.Combine(path, Path.GetFileName(job.DestinationFullPath)), true);
                        }
                    }
                    catch (Exception ex)
                    {
                        string msg = $"Error copying output file to other locations for {job.Name}";
                        logger.LogException(ex, msg);
                        job.SetError(msg);
                        return;
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                // DELETE SOURCE FILE
                if (job.PostProcessingFlags.HasFlag(PostProcessingFlags.DeleteSourceFile))
                {
                    try
                    {
                        File.Delete(job.SourceFullPath);
                    }
                    catch (Exception ex)
                    {
                        string msg = $"Error deleting source file for {job.Name}";
                        logger.LogException(ex, msg);
                        job.SetError(msg);
                        return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                job.ResetStatus();
                logger.LogInfo($"PostProcess was cancelled for {job}");
                return;
            }
            catch (Exception ex)
            {
                string msg = $"Error postprocessing {job}";
                logger.LogException(ex, msg);
                job.SetError(msg);
                return;
            }

            job.CompletePostProcessing();
            logger.LogInfo($"Successfully post-processed {job.Name} encoding job.");
        }
    }
}
