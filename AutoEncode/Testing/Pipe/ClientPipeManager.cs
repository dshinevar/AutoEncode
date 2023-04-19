using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Messages;
using AutoEncodeUtilities.Json;
using H.Formatters;
using H.Pipes;
using H.Pipes.Args;
using H.Pipes.Extensions;
using Newtonsoft.Json;

namespace Testing.Pipe
{
    public class ClientPipeManager : IClientPipeManager, IDisposable
    {
        private PipeClient<AEMessage>? ClientPipe;

        public ClientPipeManager()
        {
            SetupClientPipe();
        }

        public bool IsConnected => ClientPipe?.IsConnected ?? false;

        private async void SetupClientPipe()
        {
            ClientPipe = new PipeClient<AEMessage>(CommonConstants.PipeName, formatter: new AEJsonFormatter());
            ClientPipe.Connected += (o, args) => OnConnected(args);
            ClientPipe.Disconnected += (o, args) => OnDisconnected(args);
            ClientPipe.ExceptionOccurred += (o, args) => OnExceptionOccurred(args);
            await ClientPipe.ConnectAsync();
        }

        public void Dispose()
        {
            ClientPipe?.DisconnectAsync();
            ClientPipe?.DisposeAsync();
        }

        public async Task<EncodingJobQueueStatusMessage> GetEncodingJobQueue()
        {
            AEMessage message = await GetEncodingJobQueueAsync();

            if (message is EncodingJobQueueStatusMessage)
            {
                return (EncodingJobQueueStatusMessage)message;
            }

            return null;
        }

        private async Task<AEMessage> GetEncodingJobQueueAsync()
        {
            ClientPipe?.WriteAsync(new EncodingJobQueueRequest());

            var messageEvent = await ClientPipe?.WaitMessageAsync();

            return messageEvent.Message;
        }

        private void OnConnected(ConnectionEventArgs<AEMessage> args)
        {
            Console.WriteLine($"Connected to Server {args.Connection.PipeName}");
        }

        private void OnDisconnected(ConnectionEventArgs<AEMessage> args) 
        {
            Console.WriteLine($"Disconnected from Server {args.Connection.PipeName}");
        }

        private void OnExceptionOccurred(ExceptionEventArgs args) 
        {
            Console.WriteLine(args.Exception.ToString());
        }
    }
}
