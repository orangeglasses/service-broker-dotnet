using System.Net.Http;
using azure.Lib;
using Microsoft.Extensions.Logging;

namespace azure
{
    internal abstract class AzureClient
    {
        protected HttpClient Client { get; }

        protected IHttp Http { get; }

        protected IJson Json { get; }

        protected ILogger Log { get; }

        protected AzureClient(HttpClient client, IHttp http, IJson json, ILogger log)
        {
            Client = client;
            Http = http;
            Json = json;
            Log = log;
        }
    }
}
