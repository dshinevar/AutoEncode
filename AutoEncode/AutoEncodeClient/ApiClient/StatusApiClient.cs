using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Logger;
using RestSharp;
using RestSharp.Authenticators;

namespace AutoEncodeClient.ApiClient
{
    public class StatusApiClient : AutoEncodeApiClientBase
    {
        private static readonly string BaseUrl = ApiRouteConstants.StatusController;
        private string LoggerName = "StatusApiClient";

        public StatusApiClient(ILogger logger, string ipAddress, int port)
            : base(logger, ipAddress, port)
        {

        }

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
    }
}
