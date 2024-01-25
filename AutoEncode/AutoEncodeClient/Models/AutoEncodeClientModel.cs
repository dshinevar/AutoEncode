using AutoEncodeClient.Comm;
using AutoEncodeClient.Config;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Logger;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoEncodeClient.Models
{
    public class AutoEncodeClientModel
    {
        #region Private Properties
        private ICommunicationManager CommunicationManager { get; set; }

        private ILogger Logger { get; set; }
        private AEClientConfig Config { get; set; }
        #endregion Private Properties

        #region Properties
        #endregion Properties

        public AutoEncodeClientModel(ILogger logger, ICommunicationManager communicationManager, AEClientConfig config)
        {
            Logger = logger;
            Config = config;
            CommunicationManager = communicationManager;
            Logger.CheckAndDoRollover();
        }
    }
}
