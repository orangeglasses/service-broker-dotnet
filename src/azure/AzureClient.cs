using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace azure
{
    internal abstract class AzureClient
    {
        protected static readonly JsonSerializerSettings JsonSerializerSettings =
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new List<JsonConverter> { new StringEnumConverter() }
            };

        protected static readonly JsonSerializer JsonSerializer = new JsonSerializer
        {
            ContractResolver = JsonSerializerSettings.ContractResolver,
            Converters = { new StringEnumConverter() }
        };

        protected HttpClient Client { get; }

        protected ILogger Log { get; }

        protected AzureClient(HttpClient client, ILogger log)
        {
            Client = client;
            Log = log;
        }
    }
}
