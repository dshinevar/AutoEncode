using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Messages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoEncodeClient.Comm
{
    public partial class CommunicationManager
    {
        public async Task<(IDictionary<string, IEnumerable<SourceFileData>> Movies, IDictionary<string, IEnumerable<ShowSourceFileData>> Shows)> RequestSourceFiles()
        {
            (IDictionary<string, IEnumerable<SourceFileData>> Movies, IDictionary<string, IEnumerable<ShowSourceFileData>> Shows) returnData = (null, null);

            try
            {
                AEMessage<SourceFilesResponse> returnMessage = await SendReceiveAsync<SourceFilesResponse>(AEMessageFactory.CreateSourceFilesRequest());
                returnData = (returnMessage.Data.MovieSourceFiles, returnMessage.Data.ShowSourceFiles);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to get source files.", nameof(CommunicationManager), new { ConnectionString });
            }

            return returnData;
        }

        public async Task<bool> CancelJob(ulong jobId)
        {
            bool returnData = false;

            try
            {
                AEMessage<bool> returnMessage = await SendReceiveAsync<bool>(AEMessageFactory.CreateCancelRequest(jobId));
                returnData = returnMessage.Data;
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
                AEMessage<bool> returnMessage = await SendReceiveAsync<bool>(AEMessageFactory.CreatePauseRequest(jobId));
                returnData = returnMessage.Data;
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
                AEMessage<bool> returnMessage = await SendReceiveAsync<bool>(AEMessageFactory.CreateResumeRequest(jobId));
                returnData = returnMessage.Data;
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
                AEMessage<bool> returnMessage = await SendReceiveAsync<bool>(AEMessageFactory.CreateCancelPauseRequest(jobId));
                returnData = returnMessage.Data;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to cancel then pause job", nameof(CommunicationManager), new { jobId });
            }

            return returnData;
        }

        public async Task<bool> RequestEncode(Guid sourceFileGuid, bool isShow)
        {
            bool returnData = false;
            try
            {
                AEMessage<bool> returnMessage =  await SendReceiveAsync<bool>(AEMessageFactory.CreateEncodeRequest(sourceFileGuid, isShow));
                returnData = returnMessage.Data;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to request encode.", nameof(CommunicationManager), new { sourceFileGuid, isShow });
            }

            return returnData;
        }
    }
}
