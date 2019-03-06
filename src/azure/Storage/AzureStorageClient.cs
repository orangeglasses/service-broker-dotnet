using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using azure.Config;
using azure.Errors;
using azure.RoleAssignments.Model;
using azure.Storage.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace azure.Storage
{
    internal class AzureStorageClient : AzureClient, IAzureStorageClient
    {
        public const string DefaultStorageApiVersion = "2018-07-01";
        public const string DefaultRoleAssignmentApiVersion = "2018-01-01-preview";

        private readonly AzureOptions _azureOptions;

        public AzureStorageClient(HttpClient client, IOptions<AzureOptions> azureOptions, ILogger<AzureStorageClient> log)
            : base(client, log)
        {
            _azureOptions = azureOptions.Value;
        }

        public async Task<StorageAccount> CreateStorageAccount(
            string resourceGroupName, StorageAccount storageAccount, string apiVersion = DefaultStorageApiVersion,
            CancellationToken ct = default)
        {
            var serializedStorageAccount =
                JsonConvert.SerializeObject(storageAccount, Formatting.None, JsonSerializerSettings);

            do
            {
                var requestUri =
                    $"/subscriptions/{_azureOptions.SubscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccount.Name}?api-version={apiVersion}";
                var response = await Client.PutAsync(
                    requestUri,
                    new StringContent(serializedStorageAccount, Encoding.UTF8, "application/json"),
                    ct);

                using (response)
                {
                    Log.LogInformation(
                        $"Storage account {storageAccount.Name} created from previous request with same properties");

                    using (var responseStream = await response.Content.ReadAsStreamAsync())
                    using (var streamReader = new StreamReader(responseStream))
                    using (var jsonTextReader = new JsonTextReader(streamReader))
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var createdStorageAccount = JsonSerializer.Deserialize<StorageAccount>(jsonTextReader);
                            return createdStorageAccount;
                        }

                        if (response.StatusCode == HttpStatusCode.Accepted)
                        {
                            Log.LogInformation($"Request to create storage account {storageAccount.Name} was accepted");
                        }
                        else
                        {
                            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(jsonTextReader);
                            throw new AzureResourceException("Unexpected status code", response.StatusCode, errorResponse.Error);
                        }
                    }
                }

                await Task.Delay(100, ct);

            } while (true);
        }

        public async Task DeleteStorageAccount(
            string id, string apiVersion = DefaultStorageApiVersion, CancellationToken ct = default)
        {
            // Parse id string to leave just the part from the resource group name and further.
            var urlPart = id.Split('/').Skip(4).Aggregate((acc, part) => acc + '/' + part);

            var requestUri = $"/subscriptions/{_azureOptions.SubscriptionId}/resourceGroups/{urlPart}?api-version={apiVersion}";
            var response = await Client.DeleteAsync(requestUri, ct);
            using (response)
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Log.LogInformation($"Storage account deleted: {id}");
                }
                else if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    Log.LogInformation($"Storage account did not exist: {id}");
                }
                else
                {
                    using (var responseStream = await response.Content.ReadAsStreamAsync())
                    using (var streamReader = new StreamReader(responseStream))
                    using (var jsonTextReader = new JsonTextReader(streamReader))
                    {
                        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(jsonTextReader);
                        throw new AzureResourceException("Unexpected status code", response.StatusCode, errorResponse.Error);
                    }
                }
            }
        }

        public async Task<RoleAssignment> GrantPrincipalAccessToStorageAccount(string storageAccountId,
            Guid roleDefinitionId, Guid principalId,
            string apiVersion = DefaultRoleAssignmentApiVersion, CancellationToken ct = default)
        {
            var roleAssignmentName = Guid.NewGuid();
            var urlPart =
                $"{storageAccountId}/providers/Microsoft.Authorization/roleAssignments/" +
                $"{roleAssignmentName}?api-version={apiVersion}";
            var request = new HttpRequestMessage(HttpMethod.Put, urlPart)
            {
                Content = new StringContent(
                    JObject.FromObject(new
                    {
                        properties = JObject.FromObject(new
                        {
                            roleDefinitionId = $"/subscriptions/{_azureOptions.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/{roleDefinitionId}",
                            principalId = principalId
                        })
                    }).ToString(Formatting.None), Encoding.UTF8, "application/json")
            };

            using (request)
            using (var response = await Client.SendAsync(request, ct))
            using (var responseStream = await response.Content.ReadAsStreamAsync())
            using (var streamReader = new StreamReader(responseStream))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                if (response.StatusCode == HttpStatusCode.Created)
                {
                    var roleAssignment = JsonSerializer.Deserialize<RoleAssignment>(jsonTextReader);
                    return roleAssignment;
                }

                // No 200 response, must be an error.
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(jsonTextReader);
                throw new AzureResourceException("Unexpected response code", response.StatusCode, errorResponse.Error);
            }
        }

        public async Task<IEnumerable<StorageAccountKey>> GetStorageAccountKeys(string storageAccountId,
            string apiVersion = DefaultStorageApiVersion, CancellationToken ct = default)
        {
            var urlPart = $"{storageAccountId}/listKeys?api-version={apiVersion}";
            using (var request = new HttpRequestMessage(HttpMethod.Post, urlPart))
            using (var response = await Client.SendAsync(request, ct))
            {
                using (var responseStream = await response.Content.ReadAsStreamAsync())
                using (var streamReader = new StreamReader(responseStream))
                using (var jsonTextReader = new JsonTextReader(streamReader))
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var storageAccountListKeysResult =
                            JsonSerializer.Deserialize<StorageAccountListKeysResult>(jsonTextReader);
                        return storageAccountListKeysResult.Keys;
                    }

                    var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(jsonTextReader);
                    throw new AzureResourceException("Unexpected status code", response.StatusCode, errorResponse.Error);
                }
            }
        }

        private class StorageAccountListKeysResult
        {
            public StorageAccountKey[] Keys { get; set; }
        }
    }
}
