using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoEncodeClient.Enums
{
    public enum ClientUpdateType
    {
        None = 0,

        Queue_Update = 1,

        Status_Update = 2,

        Processing_Data_Update = 3,

        Encoding_Progress_Update = 4,
    }
}
