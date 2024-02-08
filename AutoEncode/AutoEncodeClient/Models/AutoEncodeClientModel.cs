using AutoEncodeClient.Config;
using AutoEncodeClient.Models.Interfaces;
using AutoEncodeUtilities.Logger;

namespace AutoEncodeClient.Models
{
    public class AutoEncodeClientModel : IAutoEncodeClientModel
    {
        #region Dependencies
        private ILogger Logger { get; set; }
        private AEClientConfig Config { get; set; }
        #endregion Dependencies

        #region Properties
        #endregion Properties

        public AutoEncodeClientModel() { }
    }
}
