using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using azure.Config;
using azure.Errors;
using azure.Lib;
using azure.ResourceGroups.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace azure.ResourceGroups
{
    internal class AzureResourceGroupClient : AzureClient, IAzureResourceGroupClient
    {
        public const string DefaultApiVersion = "2018-05-01";

        private readonly AzureOptions _azureOptions;

        public AzureResourceGroupClient(
            HttpClient client, IHttp http, IJson json, IOptions<AzureOptions> azureOptions,
            ILogger<AzureResourceGroupClient> log)
            : base(client, http, json, log)
        {
            _azureOptions = azureOptions.Value;
        }

        public async Task<bool> ResourceGroupExists(string name, string apiVersion = DefaultApiVersion, CancellationToken ct = default)
        {
            Log.LogDebug("Checking whether resource group exists: {name}", name);

            var requestUri = $"/subscriptions/{_azureOptions.SubscriptionId}/resourcegroups/{name}?api-version={DefaultApiVersion}";
            using (var request = new HttpRequestMessage(HttpMethod.Head, requestUri))
            {
                return await Http.SendRequestAndDecodeResponse(
                    Client,
                    request,
                    (statusCode, jsonTextReader) =>
                    {
                        switch (statusCode)
                        {
                            case HttpStatusCode.NotFound:
                                Log.LogDebug("Resource group does not exist: {name}", name);
                                return false;
                            case HttpStatusCode.NoContent:
                                Log.LogDebug("Resource group exists: {name}", name);
                                return true;
                        }

                        var errorResponse = Json.Deserialize<ErrorResponse>(jsonTextReader);
                        throw new AzureResourceException("Unexpected status code", statusCode, errorResponse.Error);
                    },
                    ct);
            }
        }

        public async Task<ResourceGroup> CreateResourceGroup(ResourceGroup resourceGroup, string apiVersion = DefaultApiVersion, CancellationToken ct = default)
        {
            var resourceGroupName = resourceGroup.Name;
            var serializedResourceGroup =
                JsonConvert.SerializeObject(resourceGroup, Formatting.None, Json.JsonSerializerSettings);
            var requestUri = $"/subscriptions/{_azureOptions.SubscriptionId}/resourcegroups/{resourceGroupName}?api-version={apiVersion}";
            var request = new HttpRequestMessage(HttpMethod.Put, requestUri)
            {
                Content = new StringContent(serializedResourceGroup, Encoding.UTF8, "application/json")
            };
            using (request)
            {
                return await Http.SendRequestAndDecodeResponse(
                    Client,
                    request,
                    (statusCode, jsonTextReader) =>
                    {
                        if (statusCode == HttpStatusCode.OK || statusCode == HttpStatusCode.Created)
                        {
                            return Json.Deserialize<ResourceGroup>(jsonTextReader);
                        }

                        var errorResponse = Json.Deserialize<ErrorResponse>(jsonTextReader);
                        throw new AzureResourceException(
                            "Unexpected status code for resource group create", statusCode, errorResponse.Error);
                    },
                    ct);
            }
        }

        public async Task<bool> DeleteResourceGroupIfEmpty(string name, string apiVersion = DefaultApiVersion, CancellationToken ct = default)
        {
            // First get all resources from group: only delete if empty.
            bool groupIsEmpty;
            var getRequestUri = $"/subscriptions/{_azureOptions.SubscriptionId}/resourcegroups/{name}/resources?api-version={apiVersion}";
            using (var request = new HttpRequestMessage(HttpMethod.Get, getRequestUri))
            {
                groupIsEmpty = await Http.SendRequestAndDecodeResponse(
                    Client,
                    request,
                    (statusCode, jsonTextReader) =>
                    {
                        if (statusCode == HttpStatusCode.OK)
                        {
                            var resourceListResult = Json.Deserialize<ResourceListResult>(jsonTextReader);
                            return !resourceListResult.Resources.Any();
                        }

                        var errorResponse = Json.Deserialize<ErrorResponse>(jsonTextReader);
                        throw new AzureResourceException(
                            "Unexpected response code for resource list get", statusCode, errorResponse.Error);
                    },
                    ct);
            }

            if (groupIsEmpty)
            {
                var requestUri = $"/subscriptions/{_azureOptions.SubscriptionId}/resourcegroups/{name}?api-version={apiVersion}";
                using (var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, requestUri))
                {
                    return await Http.SendRequestAndDecodeResponse(
                        Client,
                        deleteRequest,
                        async (statusCode, jsonTextReader) =>
                        {
                            if (statusCode == HttpStatusCode.OK)
                            {
                                Log.LogInformation($"Resource group {name} deleted");
                                return true;
                            }

                            if (statusCode == HttpStatusCode.Accepted)
                            {
                                Log.LogInformation($"Delete resource group {name} started");

                                // Wait for resource group deletion.
                                do
                                {
                                    await Task.Delay(100, ct);

                                    using (var getRequest = new HttpRequestMessage(HttpMethod.Get, requestUri))
                                    {
                                        var getResponse = await Client.SendAsync(getRequest, ct);
                                        if (getResponse.StatusCode == HttpStatusCode.NotFound)
                                        {
                                            Log.LogInformation($"Resource group {name} no longer found: must be deleted");
                                            return true;
                                        }

                                        if (getResponse.StatusCode == HttpStatusCode.OK)
                                        {
                                            Log.LogDebug($"Resource group {name} found: not yet deleted");
                                        }
                                    }
                                } while (true);
                            }

                            var errorResponse = Json.Deserialize<ErrorResponse>(jsonTextReader);
                            throw new AzureResourceException(
                                "Unexpected response code for resource group delete", statusCode, errorResponse.Error);
                        },
                        ct);
                }
            }
            return false;
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
