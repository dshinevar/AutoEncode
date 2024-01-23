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
        private CommunicationManager CommunicationManager { get; set; }

        private ILogger Logger { get; set; }
        private AEClientConfig Config { get; set; }
        #endregion Private Properties

        #region Properties
        #endregion Properties

        public AutoEncodeClientModel(ILogger logger, CommunicationManager communicationManager, AEClientConfig config)
        {
            Logger = logger;
            Config = config;
            CommunicationManager = communicationManager;
            Logger.CheckAndDoRollover();
        }

        public async Task<(IDictionary<string, IEnumerable<SourceFileData>> Movies, IDictionary<string, IEnumerable<ShowSourceFileData>> Shows)> RequestSourceFiles() => await CommunicationManager.RequestSourceFiles();

        public async Task<bool> RequestEncodingJob(Guid guid, bool isShow) => await CommunicationManager.RequestEncode(guid, isShow);
    }
}
