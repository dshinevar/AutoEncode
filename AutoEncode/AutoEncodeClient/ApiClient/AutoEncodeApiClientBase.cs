using AutoEncodeUtilities;
using AutoEncodeUtilities.Logger;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoEncodeClient.ApiClient
{
    public abstract class AutoEncodeApiClientBase
    {
        private string IpAddress { get; }
        private int Port { get; }
#if DEBUG
        private string BaseUrl => $"http://127.0.0.1:5221/{ApiRouteConstants.BaseRoute}";
#else
        private string BaseUrl => $"http://{IpAddress}:80/{ApiRouteConstants.BaseRoute}";
#endif

        protected ILogger Logger { get; }

        protected RestClient Client { get; set; }

        public AutoEncodeApiClientBase(ILogger logger, string ipAddress, int port)
        {
            Logger = logger;
            IpAddress = ipAddress;
            Port = port;
            JsonSerializerSettings settings = new()
            {
                TypeNameHandling = TypeNameHandling.All,
                NullValueHandling = NullValueHandling.Ignore,
            };
            Client = new RestClient(BaseUrl, configureSerialization: s => s.UseNewtonsoftJson(settings));
        }

        protected T Execute<T>(RestRequest request) 
        {
            RestResponse<T> response = Client.Execute<T>(request);

            if (response is not null)
            {
                if (response.IsSuccessful is false)
                {
                    response.ThrowIfError();

                    if (string.IsNullOrEmpty(response.ErrorMessage) is false)
                    {
                        throw new Exception(response.ErrorMessage);
                    }
                }
            }

            return response.Data;
        }
    }
}
