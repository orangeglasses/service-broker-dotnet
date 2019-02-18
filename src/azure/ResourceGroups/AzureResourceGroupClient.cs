using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using azure.resources;
using azure.ResourceGroups.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace azure.ResourceGroups
{
    internal class AzureResourceGroupClient : AzureClient, IAzureResourceGroupClient
    {
        public const string DefaultApiVersion = "2018-05-01";

        private readonly HttpClient _client;

        public AzureResourceGroupClient(HttpClient client, ILogger<AzureResourceGroupClient> log)
            : base(log)
        {
            _client = client;
        }

        public async Task<bool> ResourceGroupExists(string name, string apiVersion = DefaultApiVersion, CancellationToken ct = default)
        {
            Log.LogDebug("Checking whether resource group exists: {name}", name);
            using (var request = new HttpRequestMessage(HttpMethod.Head, $"{name}?api-version={DefaultApiVersion}"))
            using (var response = await _client.SendAsync(request, ct))
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.NotFound:
                        Log.LogDebug("Resource group does not exist: {name}", name);
                        return false;
                    case HttpStatusCode.NoContent:
                        Log.LogDebug("Resource group exists: {name}", name);
                        return true;
                }

                throw new AzureResourceException("Unexpected status code", response.StatusCode);
            }
        }

        public async Task<ResourceGroup> CreateResourceGroup(ResourceGroup resourceGroup, string apiVersion = DefaultApiVersion, CancellationToken ct = default)
        {
            var resourceGroupName = resourceGroup.Name;
            var serializedResourceGroup =
                JsonConvert.SerializeObject(resourceGroup, Formatting.None, JsonSerializerSettings);
            
            var response = await _client.PutAsync(
                $"{resourceGroupName}?api-version={apiVersion}",
                new StringContent(serializedResourceGroup, Encoding.UTF8, "application/json"),
                ct);
            using (response)
            {
                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
                {
                    using (var responseStream = await response.Content.ReadAsStreamAsync())
                    using (var streamReader = new StreamReader(responseStream))
                    using (var jsonTextReader = new JsonTextReader(streamReader))
                    {
                        return JsonSerializer.Deserialize<ResourceGroup>(jsonTextReader);
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                throw new AzureResourceException("Unexpected status code", response.StatusCode, errorContent);
            }
        }
    }
}
