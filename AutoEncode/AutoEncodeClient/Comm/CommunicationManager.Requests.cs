using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Messages;
using System;
using System.Collections.Generic;

namespace AutoEncodeClient.Comm
{
    public partial class CommunicationManager
    {
        public Dictionary<string, List<VideoSourceData>> GetMovieSourceData()
        {
            Dictionary<string, List<VideoSourceData>> returnData = null;

            try
            {
                AEMessage<Dictionary<string, List<VideoSourceData>>> returnMessage = SendReceive<Dictionary<string, List<VideoSourceData>>>(AEMessageFactory.CreateMovieSourceFilesRequest());
                returnData = returnMessage.Data;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to get movie source data.", nameof(CommunicationManager), new { ConnectionString });
            }

            return returnData;
        }

        public Dictionary<string, List<ShowSourceData>> GetShowSourceData()
        {
            Dictionary<string, List<ShowSourceData>> returnData = null;

            try
            {
                AEMessage<Dictionary<string, List<ShowSourceData>>> returnMessage = SendReceive<Dictionary<string, List<ShowSourceData>>>(AEMessageFactory.CreateShowSourceFilesRequest());
                returnData = returnMessage.Data;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to get show source data.", nameof(CommunicationManager), new { ConnectionString });
            }

            return returnData;
        }

        public bool CancelJob(ulong jobId)
        {
            bool returnData = false;

            try
            {
                AEMessage<bool> returnMessage = SendReceive<bool>(AEMessageFactory.CreateCancelRequest(jobId));
                returnData = returnMessage.Data;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to cancel job", nameof(CommunicationManager), new { jobId });
            }

            return returnData;
        }

        public bool PauseJob(ulong jobId)
        {
            bool returnData = false;

            try
            {
                AEMessage<bool> returnMessage = SendReceive<bool>(AEMessageFactory.CreatePauseRequest(jobId));
                returnData = returnMessage.Data;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to pause job", nameof(CommunicationManager), new { jobId });
            }

            return returnData;
        }

        public bool ResumeJob(ulong jobId)
        {
            bool returnData = false;

            try
            {
                AEMessage<bool> returnMessage = SendReceive<bool>(AEMessageFactory.CreateResumeRequest(jobId));
                returnData = returnMessage.Data;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to resume job", nameof(CommunicationManager), new { jobId });
            }

            return returnData;
        }

        public bool CancelThenPauseJob(ulong jobId)
        {
            bool returnData = false;

            try
            {
                AEMessage<bool> returnMessage = SendReceive<bool>(AEMessageFactory.CreateCancelPauseRequest(jobId));
                returnData = returnMessage.Data;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to cancel then pause job", nameof(CommunicationManager), new { jobId });
            }

            return returnData;
        }
    }
}
