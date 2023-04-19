using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Json;
using AutoEncodeUtilities.Messages;
using H.Pipes;
using H.Pipes.Args;
using H.Pipes.Extensions;
using System.Collections.Concurrent;

namespace AutoEncodeAPI.Pipe
{
    public class ClientPipeManager : IClientPipeManager, IDisposable
    {
        private PipeClient<AEMessage>? ClientPipe;
        private readonly ConcurrentDictionary<Guid, AEMessage> ReceivedMessages = new();

        public ClientPipeManager()
        {
            SetupClientPipe();
        }

        public void Dispose()
        {
            ClientPipe?.DisconnectAsync();
            ClientPipe?.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        public async Task<List<EncodingJobData>> GetEncodingJobQueueAsync()
        {
            AEMessage message = AEMessageFactory.CreateEncodingJobQueueRequest();
            ClientPipe?.WriteAsync(message);
            return await TryGetDataAsync<List<EncodingJobData>>(message.Guid, AEMessageType.Status_Queue_Response);
        }

        public async Task<Dictionary<string, List<VideoSourceData>>> GetMovieSourceFilesAsync()
        {
            AEMessage message = AEMessageFactory.CreateMovieSourceFilesRequest();
            ClientPipe?.WriteAsync(message);
            return await TryGetDataAsync<Dictionary<string, List<VideoSourceData>>>(message.Guid, AEMessageType.Status_MovieSourceFiles_Response);
        }

        public async Task<Dictionary<string, List<ShowSourceData>>> GetShowSourceFilesAsync()
        {
            AEMessage message = AEMessageFactory.CreateShowSourceFilesRequest();
            ClientPipe?.WriteAsync(message);
            return await TryGetDataAsync<Dictionary<string, List<ShowSourceData>>>(message.Guid, AEMessageType.Status_ShowSourceFiles_Response);
        }

        #region Events
        private static void OnConnected(ConnectionEventArgs<AEMessage> args)
        {
            Console.WriteLine($"Connected to Server {args.Connection.PipeName}");
        }

        private static void OnDisconnected(ConnectionEventArgs<AEMessage> args)
        {
            Console.WriteLine($"Disconnected from Server {args.Connection.PipeName}");
        }

        private static void OnExceptionOccurred(ExceptionEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
        }

        private void OnMessageReceived(ConnectionMessageEventArgs<AEMessage?> args)
        {
            AEMessage? message = args.Message;
            if (message is not null && message.IsResponse is true)
            {
                bool found = ReceivedMessages.TryGetValue(message.Guid, out AEMessage? oldMessage);
                if (found is true)
                {
                    ReceivedMessages.TryUpdate(message.Guid, message, oldMessage);
                }
                else
                {
                    ReceivedMessages.TryAdd(message.Guid, message);
                }
            }
        }
        #endregion Events

        #region Private Functions
        private async void SetupClientPipe()
        {
            ClientPipe = new PipeClient<AEMessage>(CommonConstants.PipeName, formatter: new AEJsonFormatter());
            ClientPipe.Connected += (o, args) => OnConnected(args);
            ClientPipe.Disconnected += (o, args) => OnDisconnected(args);
            ClientPipe.ExceptionOccurred += (o, args) => OnExceptionOccurred(args);
            ClientPipe.MessageReceived += (o, args) => OnMessageReceived(args);
            await ClientPipe.ConnectAsync();
        }

        /// <summary>Polls and tries to wait for a message with a matching Guid and message type</summary>
        /// <typeparam name="T">The data type expected in the message return</typeparam>
        /// <param name="guid"><see cref="Guid"/> being looked for.</param>
        /// <param name="messageType"><see cref="AEMessageType"/> being looked for.</param>
        /// <returns></returns>
        private async Task<T?> TryGetDataAsync<T>(Guid guid, AEMessageType messageType)
        {
            AEMessage? message;
            while (ReceivedMessages.TryRemove(guid, out message) is false)
            {
                await Task.Delay(100);
            }

            if (message is AEMessage<T> messageWithData && (message?.MessageType.Equals(messageType) ?? false))
            {
                return messageWithData.Data;
            }
            else
            {
                return default;
            }
        }
        #endregion Private Functions
    }
}
