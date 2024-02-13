using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Messages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoEncodeClient.Communication
{
    public partial class CommunicationManager : ICommunicationManager
    {
        public async Task<IDictionary<string, (bool IsShows, IEnumerable<SourceFileData> Files)>> RequestSourceFiles()
        {
            try
            {
                SourceFilesResponse data = await SendReceive<SourceFilesResponse>(AEMessageFactory.CreateSourceFilesRequest(), AEMessageType.Source_Files_Response);
                return data.SourceFiles;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to get source files.", nameof(CommunicationManager));
            }

            return null;
        }

        public async Task<bool> CancelJob(ulong jobId)
        {
            bool returnData = false;

            try
            {
                return await SendReceive<bool>(AEMessageFactory.CreateCancelRequest(jobId), AEMessageType.Cancel_Response);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to cancel job", nameof(CommunicationManager), new { jobId });
            }

            return returnData;
        }

        public async Task<bool> PauseJob(ulong jobId)
        {
            bool returnData = false;

            try
            {
                return await SendReceive<bool>(AEMessageFactory.CreatePauseRequest(jobId), AEMessageType.Pause_Response);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to pause job", nameof(CommunicationManager), new { jobId });
            }

            return returnData;
        }

        public async Task<bool> ResumeJob(ulong jobId)
        {
            bool returnData = false;

            try
            {
                return await SendReceive<bool>(AEMessageFactory.CreateResumeRequest(jobId), AEMessageType.Resume_Response);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to resume job", nameof(CommunicationManager), new { jobId });
            }

            return returnData;
        }

        public async Task<bool> CancelThenPauseJob(ulong jobId)
        {
            bool returnData = false;

            try
            {
                return await SendReceive<bool>(AEMessageFactory.CreateCancelPauseRequest(jobId), AEMessageType.Cancel_Pause_Response);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to cancel then pause job", nameof(CommunicationManager), new { jobId });
            }

            return returnData;
        }

        public async Task<bool> RequestEncode(Guid sourceFileGuid)
        {
            bool returnData = false;
            try
            {
                return await SendReceive<bool>(AEMessageFactory.CreateEncodeRequest(sourceFileGuid), AEMessageType.Encode_Response);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to request encode.", nameof(CommunicationManager), new { sourceFileGuid });
            }

            return returnData;
        }

        public async Task<bool> RequestRemoveJob(ulong jobId)
        {
            bool returnData = false;

            try
            {
                return await SendReceive<bool>(AEMessageFactory.CreateRemoveJobRequest(jobId), AEMessageType.Remove_Job_Response);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to remove job from queue.", nameof(CommunicationManager), new { jobId });
            }

            return returnData;
        }

        public async Task<IEnumerable<EncodingJobData>> RequestJobQueue()
        {
            IEnumerable<EncodingJobData> returnData = null;

            try
            {
                return await SendReceive<IEnumerable<EncodingJobData>>(AEMessageFactory.CreateJobQueueRequest(), AEMessageType.Job_Queue_Response);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to remove job from queue.", nameof(CommunicationManager));
            }

            return returnData;
        }
    }
}
