using AutoEncodeClient.Communication.Interfaces;
using AutoEncodeUtilities.Communication.Data;
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
        Dictionary<string, IEnumerable<SourceFileData>> sourceFiles = null;

        try
        {
            CommunicationMessage<ResponseMessageType> responseMessage = await SendReceiveAsync(RequestMessageFactory.CreateSourceFilesRequest());
            ValidateResponse(responseMessage, ResponseMessageType.SourceFilesResponse);
            sourceFiles = responseMessage.UnpackData<Dictionary<string, IEnumerable<SourceFileData>>>();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Failed to get source files.", nameof(CommunicationMessageHandler));
        }

        return sourceFiles;
    }

    public async Task<bool> RequestCancelJob(ulong jobId)
    {
        bool response = false;

        try
        {
            CommunicationMessage<ResponseMessageType> responseMessage = await SendReceiveAsync(RequestMessageFactory.CreateCancelJobRequest(jobId));
            ValidateResponse(responseMessage, ResponseMessageType.CancelResponse);
            response = responseMessage.UnpackData<bool>();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Failed to request job cancel.", nameof(CommunicationMessageHandler), new { jobId });
        }

        return response;
    }

    public async Task<bool> RequestPauseJob(ulong jobId)
    {
        bool response = false;

        try
        {
            CommunicationMessage<ResponseMessageType> responseMessage = await SendReceiveAsync(RequestMessageFactory.CreatePauseJobRequest(jobId));
            ValidateResponse(responseMessage, ResponseMessageType.PauseResponse);
            response = responseMessage.UnpackData<bool>();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Failed to request job pause.", nameof(CommunicationMessageHandler), new { jobId });
        }

        return response;
    }

    public async Task<bool> RequestResumeJob(ulong jobId)
    {
        bool response = false;

        try
        {
            CommunicationMessage<ResponseMessageType> responseMessage = await SendReceiveAsync(RequestMessageFactory.CreateResumeJobRequest(jobId));
            ValidateResponse(responseMessage, ResponseMessageType.ResumeResponse);
            response = responseMessage.UnpackData<bool>();

        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Failed to request job resume.", nameof(CommunicationMessageHandler), new { jobId });
        }

        return response;
    }

    public async Task<bool> RequestPauseAndCancelJob(ulong jobId)
    {
        bool response = false;

        try
        {
            CommunicationMessage<ResponseMessageType> responseMessage = await SendReceiveAsync(RequestMessageFactory.CreatePauseAndCancelRequest(jobId));
            ValidateResponse(responseMessage, ResponseMessageType.PauseCancelResponse);
            response = responseMessage.UnpackData<bool>();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Failed to request job pause and cancel.", nameof(CommunicationMessageHandler), new { jobId });
        }

        return response;
    }

    public async Task<bool> RequestRemoveJob(ulong jobId)
    {
        bool response = false;

        try
        {
            CommunicationMessage<ResponseMessageType> responseMessage = await SendReceiveAsync(RequestMessageFactory.CreateRemoveJobRequest(jobId));
            ValidateResponse(responseMessage, ResponseMessageType.RemoveJobResponse);
            response = responseMessage.UnpackData<bool>();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Failed to request job removal.", nameof(CommunicationMessageHandler), new { jobId });
        }

        return response;
    }

    public async Task<bool> RequestEncode(Guid sourceFileGuid)
    {
        bool response = false;

        try
        {
            CommunicationMessage<ResponseMessageType> responseMessage = await SendReceiveAsync(RequestMessageFactory.CreateEncodeRequest(sourceFileGuid));
            ValidateResponse(responseMessage, ResponseMessageType.EncodeResponse);
            response = responseMessage.UnpackData<bool>();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Failed to request job encode.", nameof(CommunicationMessageHandler), new { sourceFileGuid });
        }

        return response;
    }

    public async Task<IEnumerable<string>> BulkRequestEncode(IEnumerable<Guid> sourceFileGuids)
    {
        IEnumerable<string> response = null;

        try
        {
            CommunicationMessage<ResponseMessageType> responseMessage = await SendReceiveAsync(RequestMessageFactory.CreateBulkEncodeRequest(sourceFileGuids));
            ValidateResponse(responseMessage, ResponseMessageType.BulkEncodeResponse);
            response = responseMessage.UnpackData<IEnumerable<string>>();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Failed to bulk request job encode.", nameof(CommunicationMessageHandler), new { sourceFileGuids });
        }

        return response;
    }

    public async Task<IEnumerable<EncodingJobData>> RequestJobQueue()
    {
        IEnumerable<EncodingJobData> response = null;

        try
        {
            CommunicationMessage<ResponseMessageType> responseMessage = await SendReceiveAsync(RequestMessageFactory.CreateJobQueueRequest());
            ValidateResponse(responseMessage, ResponseMessageType.JobQueueResponse);
            response = responseMessage.UnpackData<IEnumerable<EncodingJobData>>();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Failed to request job queue.", nameof(CommunicationMessageHandler));
        }

        return response;
    }
}
