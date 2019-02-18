using System.Collections.Generic;
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

        protected ILogger Log { get; }

        protected AzureClient(ILogger log)
        {
            Log = log;
        }
    }
}
