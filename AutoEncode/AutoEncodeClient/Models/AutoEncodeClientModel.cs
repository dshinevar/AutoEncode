using AutoEncodeClient.Comm;
using AutoEncodeClient.Config;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Logger;
using System;
using System.Collections.Generic;

namespace AutoEncodeClient.Models
{
    public class AutoEncodeClientModel : IDisposable
    {
        #region Private Properties
        private CommunicationManager CommunicationManager { get; set; }

        private ILogger Logger { get; set; }
        private AEClientConfig Config { get; set; }
        #endregion Private Properties

        #region Properties
        #endregion Properties

        public AutoEncodeClientModel(ILogger logger, AEClientConfig config)
        {
            Logger = logger;
            Config = config;
            CommunicationManager = new(logger, Config.ConnectionSettings.IPAddress, Config.ConnectionSettings.CommunicationPort);
            Logger.CheckAndDoRollover();
        }

        public void Dispose()
        {
            CommunicationManager?.Dispose();
        }


        public Dictionary<string, List<VideoSourceData>> GetCurrentMovieSourceData() => CommunicationManager.GetMovieSourceData();

        public Dictionary<string, List<ShowSourceData>> GetCurrentShowSourceData() => CommunicationManager.GetShowSourceData();
    }
}
