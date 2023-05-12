using AutoEncodeServer.TaskFactory;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace AutoEncodeServer
{
    public partial class AEServerMainThread
    {
        private Task EncodingJobBuilderTask { get; set; }
        private CancellationTokenSource EncodingJobBuilderCancellationToken { get; set; }

        private Task EncodingTask { get; set; }
        private CancellationTokenSource EncodingCancellationToken { get; set; }

        private Task EncodingJobPostProcessingTask { get; set; }
        private CancellationTokenSource EncodingJobPostProcessingCancellationToken { get; set; }


        private static void CleanupJob(EncodingJob job)
        {
            if (job.Cancelled is true)
            {
                job.ResetCancel();
            }

            // If complete, no point in pausing, just "resume"
            if (job.Complete is false)
            {
                if (job.ToBePaused is true)
                {
                    job.Pause();
                }
            }
            else
            {
                job.Resume();
            }
        }

        /// <summary>Server timer task: Send update to client; Spin up threads for other tasks</summary>
        private void OnEncodingJobTaskTimerElapsed(object obj)
        {
            if (EncodingJobQueue.Any())
            {
                // Check if task is done (or null -- first time setup)
                if (EncodingJobBuilderTask?.IsCompletedSuccessfully ?? true)
                {
                    EncodingJob jobToBuild = EncodingJobQueue.GetNextEncodingJobWithStatus(EncodingJobStatus.NEW);
                    if (jobToBuild is not null)
                    {
                        EncodingJobBuilderCancellationToken = new CancellationTokenSource();
                        jobToBuild.TaskCancellationTokenSource = EncodingJobBuilderCancellationToken;

                        EncodingJobBuilderTask = Task.Factory.StartNew(()
                            => EncodingJobTaskFactory.BuildEncodingJob(jobToBuild, State.GlobalJobSettings.DolbyVisionEncodingEnabled, State.ServerSettings.FFmpegDirectory,
                                                                        State.ServerSettings.HDR10PlusExtractorFullPath, State.ServerSettings.DolbyVisionExtractorFullPath,
                                                                        State.ServerSettings.X265FullPath,
                                                                        Logger, EncodingJobBuilderCancellationToken.Token), EncodingJobBuilderCancellationToken.Token)
                                                        .ContinueWith(t => CleanupJob(jobToBuild));
                    }
                }
                
                // Check if task is done (or null -- first time setup)
                if (EncodingTask?.IsCompletedSuccessfully ?? true)
                {
                    EncodingJob jobToEncode = EncodingJobQueue.GetNextEncodingJobWithStatus(EncodingJobStatus.BUILT);
                    if (jobToEncode is not null)
                    {
                        EncodingCancellationToken = new CancellationTokenSource();
                        jobToEncode.TaskCancellationTokenSource = EncodingCancellationToken;

                        if (State.GlobalJobSettings.DolbyVisionEncodingEnabled is true && jobToEncode.EncodingInstructions.VideoStreamEncodingInstructions.HasDolbyVision is true)
                        {
                            EncodingTask = Task.Factory.StartNew(()
                                => EncodingJobTaskFactory.EncodeWithDolbyVision(jobToEncode, State.ServerSettings.FFmpegDirectory, State.ServerSettings.MkvMergeFullPath,
                                                                                Logger, EncodingCancellationToken.Token), EncodingCancellationToken.Token)
                                                            .ContinueWith(t => CleanupJob(jobToEncode));
                        }
                        else
                        {
                            EncodingTask = Task.Factory.StartNew(()
                                => EncodingJobTaskFactory.Encode(jobToEncode, State.ServerSettings.FFmpegDirectory, Logger, EncodingCancellationToken.Token), EncodingCancellationToken.Token)
                                                            .ContinueWith(t => CleanupJob(jobToEncode));
                        }
                    }
                }

                if (EncodingJobPostProcessingTask?.IsCompletedSuccessfully ?? true)
                {
                    EncodingJob jobToPostProcess = EncodingJobQueue.GetNextEncodingJobForPostProcessing();
                    if (jobToPostProcess is not null)
                    {
                        EncodingJobPostProcessingCancellationToken = new CancellationTokenSource();
                        jobToPostProcess.TaskCancellationTokenSource = EncodingJobPostProcessingCancellationToken;

                        EncodingJobPostProcessingTask = Task.Factory.StartNew(()
                            => EncodingJobTaskFactory.PostProcess(jobToPostProcess, Logger, EncodingJobPostProcessingCancellationToken.Token), EncodingJobPostProcessingCancellationToken.Token)
                                                        .ContinueWith(t => CleanupJob(jobToPostProcess));
                    }
                }
            }
        }
    }
}
