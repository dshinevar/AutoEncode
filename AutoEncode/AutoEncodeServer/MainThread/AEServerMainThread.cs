using AutoEncodeServer.Communication;
using AutoEncodeServer.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Config;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Logger;
using AutoEncodeUtilities.Messages;
using NetMQ;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AutoEncodeServer.MainThread
{
    public partial class AEServerMainThread : IAEServerMainThread
    {
        #region Private Properties
        private bool _initialized = false;

        private ManualResetEvent _shutdownMRE = null;
        #endregion Private Properties

        #region Shutdown MREs
        private readonly ManualResetEvent _encodingJobFinderShutdown = new(false);
        private readonly ManualResetEvent _encodingManagerShutdown = new(false);
        #endregion Shutdown MREs

        // TODO: Handle these better
        /// <summary>Config as in file </summary>
        private AEServerConfig Config { get; set; }
        /// <summary>Config to be used; Does not have to match what is saved to file</summary>
        private AEServerConfig State { get; set; }

        #region Dependencies
        public ILogger Logger { get; set; }

        public ICommunicationManager CommunicationManager { get; set; }

        public IEncodingJobFinderThread EncodingJobFinderThread { get; set; }

        public IEncodingJobManager EncodingJobManager { get; set; }
        #endregion Dependencies

        public readonly string ThreadName = "MainThread";

        /// <summary> Constructor </summary>
        public AEServerMainThread() { }

        #region START/SHUTDOWN FUNCTIONS
        public void Initialize(AEServerConfig serverState, AEServerConfig serverConfig, ManualResetEvent shutdown)
        {
            if (_initialized is false)
            {
                try
                {
                    Debug.WriteLine($"{nameof(AEServerMainThread)} Initializing");

                    _shutdownMRE = shutdown;

                    State = serverState;
                    Config = serverConfig;

                    EncodingJobManager.Initialize(serverState, _encodingManagerShutdown);
                    EncodingJobFinderThread.Initialize(serverState, _encodingJobFinderShutdown);

                    CommunicationManager.MessageReceived += async (sender, args) =>
                    {
                        await Task.Run(() => ProcessMessage(args));
                    };
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, $"Failed to initialize {nameof(AEServerMainThread)}", ThreadName);
                    throw;
                }

                _initialized = true;
            }
        }

        public void Start()
        {
            if (_initialized is false) throw new Exception($"{nameof(AEServerMainThread)} is not initialized.");

            try
            {
                Debug.WriteLine($"{nameof(AEServerMainThread)} Starting");

                EncodingJobManager.Start();
                EncodingJobFinderThread.Start();

                CommunicationManager?.Start(Config.ConnectionSettings.CommunicationPort);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Failed to start {nameof(AEServerMainThread)}", ThreadName);
                throw;
            }
        }

        public void Shutdown()
        {
            Debug.WriteLine($"{nameof(AEServerMainThread)} Shutting Down.");

            try
            {
                // Stop Comms
                CommunicationManager?.Stop();

                // Stop threads
                EncodingJobManager?.Shutdown();
                EncodingJobFinderThread?.Stop();

                // Wait for threads to stop
                _encodingManagerShutdown.WaitOne();
                _encodingJobFinderShutdown.WaitOne();
                _shutdownMRE.Set();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Failed to shutdown {nameof(AEServerMainThread)}", ThreadName);
            }

        }
        #endregion START/SHUTDOWN FUNCTIONS

        #region PROCESSING
        private void ProcessMessage(AEMessageReceivedArgs args)
        {
            NetMQFrame clientAddress = args.ClientAddress;
            AEMessage message = args.Message;

            try
            {
                if (message is not null)
                {
                    switch (message.MessageType)
                    {
                        case AEMessageType.Source_Files_Request:
                        {
                            var sourceFiles = RequestSourceFiles();
                            var response = AEMessageFactory.CreateSourceFilesResponse(sourceFiles);
                            CommunicationManager.SendMessage(clientAddress, response);
                            break;
                        }
                        case AEMessageType.Cancel_Request:
                        {
                            bool success = EncodingJobManager.CancelJob(((AEMessage<ulong>)message).Data);
                            var response = AEMessageFactory.CreateCancelResponse(success);
                            CommunicationManager.SendMessage(clientAddress, response);
                            break;
                        }
                        case AEMessageType.Pause_Request:
                        {
                            bool success = EncodingJobManager.PauseJob(((AEMessage<ulong>)message).Data);
                            var response = AEMessageFactory.CreatePauseResponse(success);
                            CommunicationManager.SendMessage(clientAddress, response);
                            break;
                        }
                        case AEMessageType.Resume_Request:
                        {
                            bool success = EncodingJobManager.ResumeJob(((AEMessage<ulong>)message).Data);
                            var response = AEMessageFactory.CreateResumeResponse(success);
                            CommunicationManager.SendMessage(clientAddress, response);
                            break;
                        }
                        case AEMessageType.Cancel_Pause_Request:
                        {
                            bool success = EncodingJobManager.CancelThenPauseJob(((AEMessage<ulong>)message).Data);
                            var response = AEMessageFactory.CreateCancelPauseResponse(success);
                            CommunicationManager.SendMessage(clientAddress, response);
                            break;
                        }
                        case AEMessageType.Encode_Request:
                        {
                            Guid guid = ((AEMessage<Guid>)message).Data;
                            bool success = RequestEncodingJob(guid);
                            CommunicationManager.SendMessage(clientAddress, AEMessageFactory.CreateEncodeResponse(success));
                            break;
                        }
                        case AEMessageType.Remove_Job_Request:
                        {
                            ulong jobId = ((AEMessage<ulong>)message).Data;
                            bool success = EncodingJobManager.CancelThenPauseJob(jobId);
                            if (success is true) success = EncodingJobManager.RemoveEncodingJobById(jobId);
                            CommunicationManager.SendMessage(clientAddress, AEMessageFactory.CreateRemoveJobResponse(success));
                            break;
                        }
                        case AEMessageType.Job_Queue_Request:
                        {
                            IEnumerable<EncodingJobData> queue = EncodingJobManager.GetEncodingJobQueue();
                            CommunicationManager.SendMessage(clientAddress, AEMessageFactory.CreateJobQueueResponse(queue));
                            break;
                        }
                        default:
                        {
                            throw new NotImplementedException($"MessageType {message.MessageType} ({message.MessageType.GetDisplayName()}) is not implemented.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to process message from client.", nameof(CommunicationManager), new { clientAddress, RawMessage = message });
            }
        }

        private IDictionary<string, (bool IsShows, IEnumerable<SourceFileData> Files)> RequestSourceFiles() => EncodingJobFinderThread.RequestSourceFiles();
        private bool RequestEncodingJob(Guid guid) => EncodingJobFinderThread.RequestEncodingJob(guid);
        #endregion PROCESSING
    }
}
