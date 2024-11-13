using AutoEncodeClient.Communication.Interfaces;
using AutoEncodeUtilities.Communication.Data;
using AutoEncodeUtilities.Communication.Data.Response;
using AutoEncodeUtilities.Communication.Enums;
using AutoEncodeUtilities.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoEncodeClient.Communication;

public partial class CommunicationMessageHandler : ICommunicationMessageHandler
{
    public async Task<Dictionary<string, IEnumerable<SourceFileData>>> RequestSourceFiles()
    {
        try
        {
            CommunicationMessage responseMessage = await SendReceiveAsync(CommunicationRequestMessageFactory.CreateSourceFilesRequest());
            ValidateResponse(responseMessage, CommunicationMessageType.SourceFilesResponse);
            ConvertedMessage<SourceFilesResponse> convertedResponse = CommunicationMessage.Convert<SourceFilesResponse>(responseMessage);

            if (convertedResponse?.Data is SourceFilesResponse sourceFilesResponse)
            {
                return sourceFilesResponse.SourceFiles;
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Failed to get source files.", nameof(CommunicationMessageHandler));
        }

        return null;
    }

    public async Task<bool> RequestCancelJob(ulong jobId)
    {
        try
        {
            CommunicationMessage responseMessage = await SendReceiveAsync(CommunicationRequestMessageFactory.CreateCancelJobRequest(jobId));
            ValidateResponse(responseMessage, CommunicationMessageType.CancelResponse);
            ConvertedMessage<bool> convertedResponse = CommunicationMessage.Convert<bool>(responseMessage);

            if (convertedResponse?.Data is bool response)
            {
                return response;
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Failed to request job cancel.", nameof(CommunicationMessageHandler), new { jobId });
        }

        return false;
    }

    public async Task<bool> RequestPauseJob(ulong jobId)
    {
        try
        {
            CommunicationMessage responseMessage = await SendReceiveAsync(CommunicationRequestMessageFactory.CreatePauseJobRequest(jobId));
            ValidateResponse(responseMessage, CommunicationMessageType.PauseResponse);
            ConvertedMessage<bool> convertedResponse = CommunicationMessage.Convert<bool>(responseMessage);

            if (convertedResponse?.Data is bool response)
            {
                return response;
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Failed to request job pause.", nameof(CommunicationMessageHandler), new { jobId });
        }

        return false;
    }

    public async Task<bool> RequestResumeJob(ulong jobId)
    {
        try
        {
            CommunicationMessage responseMessage = await SendReceiveAsync(CommunicationRequestMessageFactory.CreateResumeJobRequest(jobId));
            ValidateResponse(responseMessage, CommunicationMessageType.ResumeResponse);
            ConvertedMessage<bool> convertedResponse = CommunicationMessage.Convert<bool>(responseMessage);

            if (convertedResponse?.Data is bool response)
            {
                return response;
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Failed to request job resume.", nameof(CommunicationMessageHandler), new { jobId });
        }

        return false;
    }

    public async Task<bool> RequestPauseAndCancelJob(ulong jobId)
    {
        try
        {
            CommunicationMessage responseMessage = await SendReceiveAsync(CommunicationRequestMessageFactory.CreatePauseAndCancelRequest(jobId));
            ValidateResponse(responseMessage, CommunicationMessageType.PauseCancelResponse);
            ConvertedMessage<bool> convertedResponse = CommunicationMessage.Convert<bool>(responseMessage);

            if (convertedResponse?.Data is bool response)
            {
                return response;
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Failed to request job pause and cancel.", nameof(CommunicationMessageHandler), new { jobId });
        }

        return false;
    }

    public async Task<bool> RequestEncode(Guid sourceFileGuid)
    {
        try
        {
            CommunicationMessage responseMessage = await SendReceiveAsync(CommunicationRequestMessageFactory.CreateEncodeRequest(sourceFileGuid));
            ValidateResponse(responseMessage, CommunicationMessageType.EncodeResponse);
            ConvertedMessage<bool> convertedResponse = CommunicationMessage.Convert<bool>(responseMessage);

            if (convertedResponse?.Data is bool response)
            {
                return response;
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Failed to request job encode.", nameof(CommunicationMessageHandler), new { sourceFileGuid });
        }

        return false;
    }

    public async Task<IEnumerable<string>> BulkRequestEncode(IEnumerable<Guid> sourceFileGuids)
    {
        try
        {
            CommunicationMessage responseMessage = await SendReceiveAsync(CommunicationRequestMessageFactory.CreateBulkEncodeRequest(sourceFileGuids));
            ValidateResponse(responseMessage, CommunicationMessageType.BulkEncodeResponse);
            ConvertedMessage<IEnumerable<string>> convertedResponse = CommunicationMessage.Convert<IEnumerable<string>>(responseMessage);
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Failed to bulk request job encode.", nameof(CommunicationMessageHandler), new { sourceFileGuids });
        }

        return null;
    }

    public async Task<bool> RequestRemoveJob(ulong jobId)
    {
        try
        {
            CommunicationMessage responseMessage = await SendReceiveAsync(CommunicationRequestMessageFactory.CreateRemoveJobRequest(jobId));
            ValidateResponse(responseMessage, CommunicationMessageType.RemoveJobResponse);
            ConvertedMessage<bool> convertedResponse = CommunicationMessage.Convert<bool>(responseMessage);

            if (convertedResponse?.Data is bool response)
            {
                return response;
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Failed to request job removal.", nameof(CommunicationMessageHandler), new { jobId });
        }

        return false;
    }

    public async Task<IEnumerable<EncodingJobData>> RequestJobQueue()
    {
        try
        {
            CommunicationMessage responseMessage = await SendReceiveAsync(CommunicationRequestMessageFactory.CreateJobQueueRequest());
            ValidateResponse(responseMessage, CommunicationMessageType.JobQueueResponse);
            ConvertedMessage<IEnumerable<EncodingJobData>> convertedResponse = CommunicationMessage.Convert<IEnumerable<EncodingJobData>>(responseMessage);

            if (convertedResponse?.Data is IEnumerable<EncodingJobData> response)
            {
                return response;
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Failed to request job queue.", nameof(CommunicationMessageHandler));
        }

        return null;
    }
}
