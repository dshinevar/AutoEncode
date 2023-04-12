﻿using AutoEncodeUtilities;
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
        private string BaseUrl => $"http://api.autoencode.com/{ApiRouteConstants.BaseRoute}"; //=> $"http://{IpAddress}:{Port}/{ApiRouteConstants.BaseRoute}";
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
