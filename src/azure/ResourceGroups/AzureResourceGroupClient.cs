using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Linq;
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

        public AzureResourceGroupClient(HttpClient client, ILogger<AzureResourceGroupClient> log)
            : base(client, log)
        {
        }

        public async Task<bool> ResourceGroupExists(string name, string apiVersion = DefaultApiVersion, CancellationToken ct = default)
        {
            Log.LogDebug("Checking whether resource group exists: {name}", name);
            using (var request = new HttpRequestMessage(HttpMethod.Head, $"{name}?api-version={DefaultApiVersion}"))
            using (var response = await Client.SendAsync(request, ct))
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
            
            var response = await Client.PutAsync(
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

        public async Task DeleteResourceGroupIfEmpty(string name, string apiVersion = DefaultApiVersion, CancellationToken ct = default)
        {
            // First get all resources from group: only delete if empty.
            var groupIsEmpty = false;
            using (var request = new HttpRequestMessage(HttpMethod.Get, $"{name}/resources?api-version={apiVersion}"))
            {
                using (var response = await Client.SendAsync(request, ct))
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var resourceListResult = await DeserializeResponse<ResourceListResult>(response);
                        if (!resourceListResult.Resources.Any())
                        {
                            groupIsEmpty = true;
                        }
                    }
                }
            }

            if (groupIsEmpty)
            {
                using (var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"{name}?api-version={apiVersion}"))
                {
                    using (var deleteResponse = await Client.SendAsync(deleteRequest, ct))
                    {
                        if (deleteResponse.StatusCode == HttpStatusCode.OK)
                        {
                            Log.LogInformation($"Resource group {name} deleted");
                        }
                        else if (deleteResponse.StatusCode == HttpStatusCode.Accepted)
                        {
                            Log.LogInformation($"Delete resource group {name} started");

                            // Wait for resource group deletion.
                            do
                            {
                                await Task.Delay(100, ct);

                                using (var getRequest = new HttpRequestMessage(HttpMethod.Get, $"{name}?api-version={apiVersion}"))
                                {
                                    var getResponse = await Client.SendAsync(getRequest, ct);
                                    if (getResponse.StatusCode == HttpStatusCode.NotFound)
                                    {
                                        Log.LogInformation($"Resource group {name} no longer found: must be deleted");
                                        return;
                                    }

                                    if (getResponse.StatusCode == HttpStatusCode.OK)
                                    {
                                        Log.LogDebug($"Resource group {name} found: not yet deleted");
                                    }
                                }
                            } while (true);
                        }
                    }
                }
            }
        }

        private static async Task<T> DeserializeResponse<T>(HttpResponseMessage response)
        {
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var streamReader = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                return JsonSerializer.Deserialize<T>(jsonTextReader);
            }
        }

        [JsonObject]
        private class ResourceListResult
        {
            [JsonProperty("value")]
            public GenericResource[] Resources { get; set; }
        }

        [JsonObject]
        private class GenericResource
        {
            [JsonProperty]
            public string Id { get; set; }
        }
    }
}
