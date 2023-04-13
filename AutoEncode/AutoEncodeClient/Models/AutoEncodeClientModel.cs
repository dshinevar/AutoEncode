using AutoEncodeClient.ApiClient;
using AutoEncodeClient.Config;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoEncodeClient.Models
{
    public class AutoEncodeClientModel
    {
        #region Private Properties
        private StatusApiClient StatusApiClient { get; set; }

        private ILogger Logger { get; set; }
        private AEClientConfig Config { get; set; }
        #endregion Private Properties

        #region Properties
        #endregion Properties

        public AutoEncodeClientModel(ILogger logger, AEClientConfig config)
        {
            Logger = logger;
            Config = config;
            StatusApiClient = new StatusApiClient(logger, 
                                                    config.ConnectionSettings.IPAddress, 
                                                    config.ConnectionSettings.Port);

            Logger.CheckAndDoRollover();   
        }

        public List<EncodingJobData> GetCurrentEncodingJobQueue()
        {
            List<EncodingJobData> encodingJobQueue = null;
            try
            {
                encodingJobQueue = StatusApiClient.GetEncodingJobQueueCurrentState();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to get current encoding job queue");
            }

            return encodingJobQueue;
        }

        public Dictionary<string, List<VideoSourceData>> GetCurrentMovieSourceData() 
        {
            Dictionary<string, List<VideoSourceData>> movieSourceFiles = null;
            try
            {
                movieSourceFiles = StatusApiClient.GetMovieSourceData();
            }
            catch (Exception ex) 
            {
                Logger.LogException(ex, "Failed to get movie source files.");
            }

            return movieSourceFiles;
        }

        public Dictionary<string, List<ShowSourceData>> GetCurrentShowSourceData()
        {
            Dictionary<string, List<ShowSourceData>> showSourceData = null;
            try
            {
                showSourceData = StatusApiClient.GetShowSourceData();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to get show source files.");
            }

            return showSourceData;
        }
    }
}
