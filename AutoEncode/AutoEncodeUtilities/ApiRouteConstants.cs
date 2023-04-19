using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoEncodeUtilities
{
    public static class ApiRouteConstants
    {
        public const string BaseRoute = "AutoEncodeAPI";

        public const string StatusController = "status";

        public const string StatusControllerBase = $"{BaseRoute}/{StatusController}";
    }
}
