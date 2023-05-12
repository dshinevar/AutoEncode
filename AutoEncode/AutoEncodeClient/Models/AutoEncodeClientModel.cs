using AutoEncodeClient.Comm;
using AutoEncodeClient.Config;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Logger;
using System;
using System.Collections.Generic;

namespace AutoEncodeClient.Models
{
    public class AutoEncodeClientModel
    {
        #region Private Properties
        private CommunicationManager CommunicationManager { get; set; }

        private ILogger Logger { get; set; }
        private AEClientConfig Config { get; set; }
        #endregion Private Properties

        #region Properties
        public bool ConnectedToServer => CommunicationManager?.Connected ?? false;
        #endregion Properties

        public AutoEncodeClientModel(ILogger logger, CommunicationManager communicationManager, AEClientConfig config)
        {
            Logger = logger;
            Config = config;
            CommunicationManager = communicationManager;
            Logger.CheckAndDoRollover();
        }

        public Dictionary<string, List<VideoSourceData>> GetCurrentMovieSourceData() => CommunicationManager.GetMovieSourceData();

        public Dictionary<string, List<ShowSourceData>> GetCurrentShowSourceData() => CommunicationManager.GetShowSourceData();
    }
}
