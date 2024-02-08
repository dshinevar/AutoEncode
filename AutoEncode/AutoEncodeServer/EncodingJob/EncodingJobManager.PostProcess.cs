using AutoEncodeServer.Interfaces;
using AutoEncodeUtilities.Enums;
using System;
using System.IO;
using System.Threading;

namespace AutoEncodeServer.EncodingJob
{
    public partial class EncodingJobManager : IEncodingJobManager
    {
        /// <summary> Runs post-processing tasks marked for the encoding job. </summary>
        /// <param name="job">The <see cref="IEncodingJobModel"/> to be post-processed.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        private void PostProcess(IEncodingJobModel job, CancellationToken cancellationToken)
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
                            string copyDestinationDirectory = Path.GetDirectoryName(path);
                            if (Directory.Exists(copyDestinationDirectory) is false)
                            {
                                Directory.CreateDirectory(copyDestinationDirectory);
                            }

                            File.Copy(job.DestinationFullPath, path, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        job.SetError(Logger.LogException(ex, $"Error copying output file to other locations for {job}",
                            details: new { job.Id, job.Name, job.PostProcessingSettings.CopyFilePaths, job.DestinationFullPath }));
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
                        job.SetError(Logger.LogException(ex, $"Error deleting source file for {job}", details: new { job.Id, job.Name, job.SourceFullPath }));
                        return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning($"Post-Process was cancelled for {job}");
                //job.ResetStatus();
                return;
            }
            catch (Exception ex)
            {
                job.SetError(Logger.LogException(ex, $"Error post-processing {job}", details: new { job.Id, job.Name, job.Status }));
                return;
            }

            job.CompletePostProcessing();
            Logger.LogInfo($"Successfully post-processed {job} encoding job.");
        }
    }
}
