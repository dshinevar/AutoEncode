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
    private readonly BlockingCollection<(NetMQFrame, CommunicationMessage)> _messages = [];
    private const int _messageProcessorTimeout = 3600000;   // 1 hour

    private Task StartMessageProcessor()
        => _messageProcessingTask = Task.Run(() =>
        {
            while (_messages.TryTake(out (NetMQFrame ClientAddress, CommunicationMessage Message) clientMessage, _messageProcessorTimeout, _shutdownCancellationTokenSource.Token))
            {
                try
                {
                    NetMQFrame clientAddress = clientMessage.ClientAddress;
                    CommunicationMessage message = clientMessage.Message;

                    switch (message.MessageType)
                    {
                        case CommunicationMessageType.SourceFilesRequest:
                        {
                            var sourceFiles = _sourceFileManager.RequestSourceFiles();
                            var response = CommunicationResponseMessageFactory.CreateSourceFilesResponse(sourceFiles);
                            _communicationMessageHandler.SendMessage(clientAddress, response);
                            break;
                        }
                        case CommunicationMessageType.CancelRequest:
                        {
                            ConvertedMessage<ulong> convertedMessage = CommunicationMessage.Convert<ulong>(message);
                            bool success = _encodingJobManager.AddCancelJobByIdRequest(convertedMessage.Data);
                            var response = CommunicationResponseMessageFactory.CreateCancelResponse(success);
                            _communicationMessageHandler.SendMessage(clientAddress, response);
                            break;
                        }
                        case CommunicationMessageType.PauseRequest:
                        {
                            ConvertedMessage<ulong> convertedMessage = CommunicationMessage.Convert<ulong>(message);
                            bool success = _encodingJobManager.AddPauseJobByIdRequest(convertedMessage.Data);
                            var response = CommunicationResponseMessageFactory.CreatePauseResponse(success);
                            _communicationMessageHandler.SendMessage(clientAddress, response);
                            break;
                        }
                        case CommunicationMessageType.ResumeRequest:
                        {
                            ConvertedMessage<ulong> convertedMessage = CommunicationMessage.Convert<ulong>(message);
                            bool success = _encodingJobManager.AddResumeJobByIdRequest(convertedMessage.Data);
                            var response = CommunicationResponseMessageFactory.CreateResumeResponse(success);
                            _communicationMessageHandler.SendMessage(clientAddress, response);
                            break;
                        }
                        case CommunicationMessageType.PauseCancelRequest:
                        {
                            ConvertedMessage<ulong> convertedMessage = CommunicationMessage.Convert<ulong>(message);
                            bool success = _encodingJobManager.AddPauseAndCancelJobByIdRequest(convertedMessage.Data);
                            var response = CommunicationResponseMessageFactory.CreatePauseAndCancelResponse(success);
                            _communicationMessageHandler.SendMessage(clientAddress, response);
                            break;
                        }
                        case CommunicationMessageType.EncodeRequest:
                        {
                            ConvertedMessage<Guid> convertedMessage = CommunicationMessage.Convert<Guid>(message);
                            bool success = _sourceFileManager.RequestEncodingJob(convertedMessage.Data);
                            _communicationMessageHandler.SendMessage(clientAddress, CommunicationResponseMessageFactory.CreateEncodeResponse(success));
                            break;
                        }
                        case CommunicationMessageType.BulkEncodeRequest:
                        {
                            ConvertedMessage<IEnumerable<Guid>> convertedMessage = CommunicationMessage.Convert<IEnumerable<Guid>>(message);
                            IEnumerable<string> failedRequests = _sourceFileManager.BulkRequestEncodingJob(convertedMessage.Data);
                            _communicationMessageHandler.SendMessage(clientAddress, CommunicationResponseMessageFactory.CreateBulkEncodeResponse(failedRequests));
                            break;
                        }
                        case CommunicationMessageType.RemoveJobRequest:
                        {
                            ConvertedMessage<ulong> convertedMessage = CommunicationMessage.Convert<ulong>(message);
                            bool success = _encodingJobManager.AddRemoveEncodingJobByIdRequest(convertedMessage.Data, RemovedEncodingJobReason.UserRequested);
                            _communicationMessageHandler.SendMessage(clientAddress, CommunicationResponseMessageFactory.CreateRemoveJobResponse(success));
                            break;
                        }
                        case CommunicationMessageType.JobQueueRequest:
                        {
                            IEnumerable<EncodingJobData> queue = _encodingJobManager.GetEncodingJobQueue();
                            _communicationMessageHandler.SendMessage(clientAddress, CommunicationResponseMessageFactory.CreateJobQueueResponse(queue));
                            break;
                        }
                        default:
                        {
                            throw new NotImplementedException($"MessageType {message.MessageType} ({message.MessageType.GetDisplayName()}) is not implemented.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, $"Error processing message.", nameof(AutoEncodeServerManager), new { clientMessage.ClientAddress, clientMessage.Message });
                }
            }
        });

    private void CommunicationMessageHandler_MessageReceived(object sender, CommunicationMessageReceivedEventArgs e)
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
