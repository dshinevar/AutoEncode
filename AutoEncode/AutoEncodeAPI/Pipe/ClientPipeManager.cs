using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Json;
using AutoEncodeUtilities.Messages;
using H.Pipes;
using H.Pipes.Args;
using H.Pipes.Extensions;

namespace AutoEncodeAPI.Pipe
{
    public class ClientPipeManager : IClientPipeManager, IDisposable
    {
        private PipeClient<AEMessage>? ClientPipe;

        public ClientPipeManager()
        {
            SetupClientPipe();
        }

        public void Dispose()
        {
            ClientPipe?.DisconnectAsync();
            ClientPipe?.DisposeAsync();
        }

        public async Task<List<EncodingJobData>> GetEncodingJobQueueAsync()
        {
            ClientPipe?.WriteAsync(AEMessageFactory.CreateEncodingJobQueueRequest());

            AEMessage message = (await ClientPipe?.WaitMessageAsync()).Message;

            (bool success, List<EncodingJobData> data) = ValidateMessageAndGetData<List<EncodingJobData>>(message, AEMessageType.Status_Queue_Response);

            if (success is true)
            {
                return data;
            }
            else
            {
                return null;
            }
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
        #endregion Events

        #region Private Functions
        private async void SetupClientPipe()
        {
            ClientPipe = new PipeClient<AEMessage>(CommonConstants.PipeName, formatter: new AEJsonFormatter());
            ClientPipe.Connected += (o, args) => OnConnected(args);
            ClientPipe.Disconnected += (o, args) => OnDisconnected(args);
            ClientPipe.ExceptionOccurred += (o, args) => OnExceptionOccurred(args);
            await ClientPipe.ConnectAsync();
        }

        private static (bool, T) ValidateMessageAndGetData<T>(AEMessage message, AEMessageType messageType)
        {
            if (message is AEMessage<T> messageWithData && message.MessageType.Equals(messageType))
            {
                return (true, messageWithData.Data);
            }
            else
            {
                return (false, default(T));
            }
        }
        #endregion Private Functions
    }
}
