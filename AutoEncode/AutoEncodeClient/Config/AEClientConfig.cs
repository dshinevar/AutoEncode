using AutoEncodeUtilities.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoEncodeClient.Config
{
    public class AEClientConfig
    {
        public ConnectionSettings ConnectionSettings { get; set; }

        public LoggerSettings LoggerSettings { get; set; }
    }
}
