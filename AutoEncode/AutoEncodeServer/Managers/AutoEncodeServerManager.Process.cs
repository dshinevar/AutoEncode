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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoEncodeServer.Managers;

public partial class AutoEncodeServerManager : IAutoEncodeServerManager
{
    private readonly BlockingCollection<(NetMQFrame, CommunicationMessage<RequestMessageType>)> _messages = [];
    private const int _messageProcessorTimeout = 3600000;   // 1 hour

    private Task StartMessageProcessor()
        => _messageProcessingTask = Task.Run(() =>
        {
            while (_messages.TryTake(out (NetMQFrame ClientAddress, CommunicationMessage<RequestMessageType> Message) clientMessage, _messageProcessorTimeout, _shutdownCancellationTokenSource.Token))
            {
                try
                {
                    NetMQFrame clientAddress = clientMessage.ClientAddress;
                    CommunicationMessage<RequestMessageType> message = clientMessage.Message;

                    switch (message.Type)
                    {
                        case RequestMessageType.SourceFilesRequest:
                        {
                            var sourceFiles = _sourceFileManager.RequestSourceFiles();
                            _communicationMessageHandler.SendMessage(clientAddress, ResponseMessageFactory.CreateSourceFilesResponse(sourceFiles));
                            break;
                        }
                        case RequestMessageType.CancelRequest:
                        {
                            ulong jobId = message.UnpackData<ulong>();
                            bool success = _encodingJobManager.AddCancelJobByIdRequest(jobId);
                            _communicationMessageHandler.SendMessage(clientAddress, ResponseMessageFactory.CreateCancelResponse(success));
                            break;
                        }
                        case RequestMessageType.PauseRequest:
                        {
                            ulong jobId = message.UnpackData<ulong>();
                            bool success = _encodingJobManager.AddPauseJobByIdRequest(jobId);
                            _communicationMessageHandler.SendMessage(clientAddress, ResponseMessageFactory.CreatePauseResponse(success));
                            break;
                        }
                        case RequestMessageType.ResumeRequest:
                        {
                            ulong jobId = message.UnpackData<ulong>();
                            bool success = _encodingJobManager.AddResumeJobByIdRequest(jobId);
                            _communicationMessageHandler.SendMessage(clientAddress, ResponseMessageFactory.CreateResumeResponse(success));
                            break;
                        }
                        case RequestMessageType.PauseCancelRequest:
                        {
                            ulong jobId = message.UnpackData<ulong>();
                            bool success = _encodingJobManager.AddPauseAndCancelJobByIdRequest(jobId);
                            _communicationMessageHandler.SendMessage(clientAddress, ResponseMessageFactory.CreatePauseAndCancelResponse(success));
                            break;
                        }
                        case RequestMessageType.RemoveJobRequest:
                        {
                            ulong jobId = message.UnpackData<ulong>();
                            bool success = _encodingJobManager.AddRemoveEncodingJobByIdRequest(jobId, RemovedEncodingJobReason.UserRequested);
                            _communicationMessageHandler.SendMessage(clientAddress, ResponseMessageFactory.CreateRemoveJobResponse(success));
                            break;
                        }
                        case RequestMessageType.EncodeRequest:
                        {
                            Guid sourceFileGuid = message.UnpackData<Guid>();
                            bool success = _sourceFileManager.RequestEncodingJob(sourceFileGuid);
                            _communicationMessageHandler.SendMessage(clientAddress, ResponseMessageFactory.CreateEncodeResponse(success));
                            break;
                        }
                        case RequestMessageType.BulkEncodeRequest:
                        {
                            IEnumerable<Guid> sourceFileGuids = message.UnpackData<IEnumerable<Guid>>();
                            IEnumerable<string> failedRequests = _sourceFileManager.BulkRequestEncodingJob(sourceFileGuids);
                            _communicationMessageHandler.SendMessage(clientAddress, ResponseMessageFactory.CreateBulkEncodeResponse(failedRequests));
                            break;
                        }
                        case RequestMessageType.JobQueueRequest:
                        {
                            IEnumerable<EncodingJobData> queue = _encodingJobManager.GetEncodingJobQueue();
                            _communicationMessageHandler.SendMessage(clientAddress, ResponseMessageFactory.CreateJobQueueResponse(queue));
                            break;
                        }
                        default:
                        {
                            throw new NotImplementedException($"MessageType {message.Type} ({message.Type.GetDisplayName()}) is not implemented.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, $"Error processing message.", nameof(AutoEncodeServerManager), new { clientMessage.ClientAddress, clientMessage.Message });
                }
            }
        });

    private void CommunicationMessageHandler_MessageReceived(object sender, RequestMessageReceivedEventArgs e)
    {
        // Restart message processing thread if stopped
        if ((_messageProcessingTask is null) ||                         // Null somehow
            (_messageProcessingTask.Status != TaskStatus.Running) ||    // Not running
            (_messageProcessingTask.IsCompleted is true))               // Completed (most likely from timeout)
        {
            StartMessageProcessor();
        }

        // Add to message queue
        _messages.TryAdd(new(e.ClientAddress, e.Message));
    }
}
