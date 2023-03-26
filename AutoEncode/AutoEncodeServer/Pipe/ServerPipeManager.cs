using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Json;
using AutoEncodeUtilities.Logger;
using AutoEncodeUtilities.Messages;
using H.Pipes;
using H.Pipes.Args;
using System;
using System.Collections.Generic;

namespace AutoEncodeServer.Pipe
{
    public class ServerPipeManager : IServerPipeManager
    {
        #region Properties
        private readonly string LoggerName = "ServerPipeManager";
        private PipeServer<AEMessage> ServerPipe { get; set; }
        private AEServerMainThread MainThread { get; set; }
        private ILogger Logger { get; set; }
        #endregion Properties

        /// <summary>Constructor</summary>
        /// <param name="mainThread">Handle of main thread.</param>
        /// <param name="logger"><see cref="ILogger"/></param>
        public ServerPipeManager(AEServerMainThread mainThread, ILogger logger)
        {
            MainThread = mainThread;
            LoggerName = $"{MainThread.ThreadName}-Pipe";
            Logger = logger;
        }

        #region Public Functions
        public async void Start()
        {
            try
            {
                ServerPipe = new PipeServer<AEMessage>(CommonConstants.PipeName, formatter: new AEJsonFormatter());
                ServerPipe.ClientConnected += (o, args) => OnClientConnected(args);
                ServerPipe.ClientDisconnected += (o, args) => OnClientDisconnected(args);
                ServerPipe.MessageReceived += (o, args) => OnMessageReceived(args);
                ServerPipe.ExceptionOccurred += (o, args) => OnExceptionOccurred(args);

                await ServerPipe.StartAsync();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error Starting Server Pipe", LoggerName);
            }
        }

        public void Stop()
        {
            try
            {
                if (ServerPipe is not null)
                {
                    if (ServerPipe.IsStarted is true)
                    {
                        ServerPipe?.StopAsync().GetAwaiter().GetResult();
                    }

                    ServerPipe?.DisposeAsync().GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error Stopping Server Pipe", LoggerName);
            }
        }
        #endregion Public Functions

        #region Private Functions
        private void OnClientConnected(ConnectionEventArgs<AEMessage> args)
        {
            Console.WriteLine($"[{LoggerName}] Client {args.Connection.PipeName} connected.");
            Logger.LogInfo($"Client {args.Connection.PipeName} connected.", LoggerName);
        }

        private void OnClientDisconnected(ConnectionEventArgs<AEMessage> args)
        {
            Console.WriteLine($"[{LoggerName}] Client {args.Connection.PipeName} disconnected.");
            Logger.LogInfo($"Client {args.Connection.PipeName} disconnected.", LoggerName);
        }

        public void OnMessageReceived(ConnectionMessageEventArgs<AEMessage> args)
        {
            try
            {
                if (args?.Message is not null)
                {
                    AEMessage msg = args.Message;

                    switch (msg.MessageType)
                    {
                        case AEMessageType.Status_Queue_Request:
                        {
                            SendQueue(EncodingJobQueue.GetEncodingJobsData());
                            break;
                        }
                        default:
                        {
                            Logger.LogWarning($"Unknown/unhandled MessageType Received: {msg.MessageType}", LoggerName);
                            break;
                        }
                    }
                }
                else
                {
                    Logger.LogError("Null message received.", LoggerName);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error handling received message.", LoggerName);
            }
        }

        private async void SendQueue(List<EncodingJobData> encodingJobQueue)
        {
            Console.WriteLine($"[{LoggerName}] Sent queue to client.");
            await ServerPipe.WriteAsync(AEMessageFactory.CreateEncodingJobQueueResponse(encodingJobQueue));
        }

        private void OnExceptionOccurred(ExceptionEventArgs args)
        {
            Logger.LogException(args.Exception, "Pipe Error", LoggerName);
            Console.WriteLine(args.Exception.ToString());
        }
        #endregion Private Functions
    }
}
