using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Messages;
using H.Pipes.Args;
using System;

namespace AutoEncodeAPI.Pipe
{
    public interface IClientPipeManager : IDisposable
    {
        Task<List<EncodingJobData>> GetEncodingJobQueueAsync();

        Task<Dictionary<string, List<VideoSourceData>>> GetMovieSourceFilesAsync();

        Task<Dictionary<string, List<ShowSourceData>>> GetShowSourceFilesAsync();
    }
}
