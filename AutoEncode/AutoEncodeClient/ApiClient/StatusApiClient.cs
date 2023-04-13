using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Logger;
using RestSharp;
using System;
using System.Collections.Generic;

namespace AutoEncodeClient.ApiClient
{
    public class StatusApiClient : AutoEncodeApiClientBase
    {
        private static readonly string BaseUrl = ApiRouteConstants.StatusController;
        private readonly string LoggerName = "StatusApiClient";

        public StatusApiClient(ILogger logger, string ipAddress, int port)
            : base(logger, ipAddress, port) { }

        public List<EncodingJobData> GetEncodingJobQueueCurrentState()
        {
            List<EncodingJobData> encodingQueue = null;
            try
            {
                RestRequest request = new()
                {
                    Resource = $"{BaseUrl}/job-queue",
                    Method = Method.Get,
                    Timeout = 20_000
                };

                encodingQueue = Execute<List<EncodingJobData>>(request);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to get Encoding Job Queue Current State.", LoggerName);
            }

            return encodingQueue;
        }

        public Dictionary<string, List<VideoSourceData>> GetMovieSourceData()
        {
            Dictionary<string, List<VideoSourceData>> sourceFiles = null;
            try
            {
                RestRequest request = new()
                {
                    Resource = $"{BaseUrl}/movie-source-files",
                    Method = Method.Get,
                    Timeout = 20_000
                };

                sourceFiles = Execute<Dictionary<string, List<VideoSourceData>>>(request);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to get Movie Source Files.", LoggerName);
            }

            return sourceFiles;
        }

        public Dictionary<string, List<ShowSourceData>> GetShowSourceData()
        {
            Dictionary<string, List<ShowSourceData>> sourceFiles = null;
            try
            {
                RestRequest request = new()
                {
                    Resource = $"{BaseUrl}/show-source-files",
                    Method = Method.Get,
                    Timeout = 20_000
                };

                sourceFiles = Execute<Dictionary<string, List<ShowSourceData>>>(request);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to get Show Source Files.", LoggerName);
            }

            return sourceFiles;
        }
    }
}
