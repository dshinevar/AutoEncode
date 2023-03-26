using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Messages;
using H.Pipes.Args;
using System;

namespace Testing.Pipe
{
    public interface IClientPipeManager : IDisposable
    {
        Task<EncodingJobQueueStatusMessage> GetEncodingJobQueue();
    }
}
