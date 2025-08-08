using AutoEncodeServer.Communication;
using AutoEncodeServer.Communication.Data;
using AutoEncodeServer.Enums;
using AutoEncodeServer.Managers.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Communication.Data;
using AutoEncodeUtilities.Communication.Enums;
using AutoEncodeUtilities.Data;
using NetMQ;
using System;
using System.Collections.Generic;

namespace AutoEncodeServer.Managers;

public partial class AutoEncodeServerManager : IAutoEncodeServerManager
{
    private void CommunicationMessageHandler_MessageReceived(object sender, RequestMessageReceivedEventArgs e)
    {
        try
        {
            Action requestAction = null;
            NetMQFrame clientAddress = e.ClientAddress;
            CommunicationMessage<RequestMessageType> message = e.Message;

            switch (message.Type)
            {
                case RequestMessageType.SourceFilesRequest:
                {
                    requestAction = () =>
                    {
                        var sourceFiles = SourceFileManager.RequestSourceFiles();
                        CommunicationMessageHandler.SendMessage(clientAddress, ResponseMessageFactory.CreateSourceFilesResponse(sourceFiles));
                    };
                    break;
                }
                case RequestMessageType.CancelRequest:
                {
                    requestAction = () =>
                    {
                        ulong jobId = message.UnpackData<ulong>();
                        bool success = EncodingJobManager.AddCancelJobByIdRequest(jobId);
                        CommunicationMessageHandler.SendMessage(clientAddress, ResponseMessageFactory.CreateCancelResponse(success));
                    };
                    break;
                }
                case RequestMessageType.PauseRequest:
                {
                    requestAction = () =>
                    {
                        ulong jobId = message.UnpackData<ulong>();
                        bool success = EncodingJobManager.AddPauseJobByIdRequest(jobId);
                        CommunicationMessageHandler.SendMessage(clientAddress, ResponseMessageFactory.CreatePauseResponse(success));
                    };
                    break;
                }
                case RequestMessageType.ResumeRequest:
                {
                    requestAction = () =>
                    {
                        ulong jobId = message.UnpackData<ulong>();
                        bool success = EncodingJobManager.AddResumeJobByIdRequest(jobId);
                        CommunicationMessageHandler.SendMessage(clientAddress, ResponseMessageFactory.CreateResumeResponse(success));
                    };

                    break;
                }
                case RequestMessageType.PauseCancelRequest:
                {
                    requestAction = () =>
                    {
                        ulong jobId = message.UnpackData<ulong>();
                        bool success = EncodingJobManager.AddPauseAndCancelJobByIdRequest(jobId);
                        CommunicationMessageHandler.SendMessage(clientAddress, ResponseMessageFactory.CreatePauseAndCancelResponse(success));
                    };
                    break;
                }
                case RequestMessageType.RemoveJobRequest:
                {
                    requestAction = () =>
                    {
                        ulong jobId = message.UnpackData<ulong>();
                        bool success = EncodingJobManager.AddRemoveEncodingJobByIdRequest(jobId, RemovedEncodingJobReason.UserRequested);
                        CommunicationMessageHandler.SendMessage(clientAddress, ResponseMessageFactory.CreateRemoveJobResponse(success));
                    };
                    break;
                }
                case RequestMessageType.EncodeRequest:
                {
                    requestAction = () =>
                    {
                        Guid sourceFileGuid = message.UnpackData<Guid>();
                        bool success = SourceFileManager.AddRequestEncodingJobForSourceFileRequest(sourceFileGuid);
                        CommunicationMessageHandler.SendMessage(clientAddress, ResponseMessageFactory.CreateEncodeResponse(success));
                    };
                    break;
                }
                case RequestMessageType.BulkEncodeRequest:
                {
                    requestAction = () =>
                    {
                        IEnumerable<Guid> sourceFileGuids = message.UnpackData<IEnumerable<Guid>>();
                        bool success = SourceFileManager.AddBulkRequestEncodingJobRequest(sourceFileGuids);
                        CommunicationMessageHandler.SendMessage(clientAddress, ResponseMessageFactory.CreateBulkEncodeResponse(success));
                    };
                    break;
                }
                case RequestMessageType.JobQueueRequest:
                {
                    requestAction = () =>
                    {
                        IEnumerable<EncodingJobData> queue = EncodingJobManager.GetEncodingJobQueue();
                        CommunicationMessageHandler.SendMessage(clientAddress, ResponseMessageFactory.CreateJobQueueResponse(queue));
                    };
                    break;
                }
                default:
                {
                    throw new NotImplementedException($"MessageType {message.Type} ({message.Type.GetDisplayName()}) is not implemented.");
                }
            }

            if (requestAction is not null)
            {
                Requests.TryAdd(requestAction);
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Error handling received communication message.", nameof(AutoEncodeServerManager), new { EventArgs = e });
        }
    }
}
