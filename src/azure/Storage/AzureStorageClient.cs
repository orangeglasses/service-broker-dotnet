using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using azure.Config;
using azure.Errors;
using azure.Lib;
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

        public AzureStorageClient(HttpClient client, IHttp http, IJson json, IOptions<AzureOptions> azureOptions, ILogger<AzureStorageClient> log)
            : base(client, http, json, log)
        {
            _azureOptions = azureOptions.Value;
        }

        public async Task<StorageAccount> CreateStorageAccount(
            string resourceGroupName, StorageAccount storageAccount, string apiVersion = DefaultStorageApiVersion,
            CancellationToken ct = default)
        {
            var serializedStorageAccount =
                JsonConvert.SerializeObject(storageAccount, Formatting.None, Json.JsonSerializerSettings);

            StorageAccount createdStorageAccount;
            do
            {
                var requestUri =
                    $"/subscriptions/{_azureOptions.SubscriptionId}" +
                    $"/resourceGroups/{resourceGroupName}" +
                    $"/providers/Microsoft.Storage/storageAccounts/{storageAccount.Name}?api-version={apiVersion}";
                var request = new HttpRequestMessage(HttpMethod.Put, requestUri)
                {
                    Content = new StringContent(serializedStorageAccount, Encoding.UTF8, "application/json")
                };

                using (request)
                {
                    createdStorageAccount = await Http.SendRequestAndDecodeResponse(
                        Client,
                        request,
                        (statusCode, jsonTextReader) =>
                        {
                            if (statusCode == HttpStatusCode.OK)
                            {
                                return Json.Deserialize<StorageAccount>(jsonTextReader);
                            }

                            if (statusCode == HttpStatusCode.Accepted)
                            {
                                Log.LogInformation($"Request to create storage account {storageAccount.Name} was accepted");
                                return null;
                            }

                            var errorResponse = Json.Deserialize<ErrorResponse>(jsonTextReader);
                            throw new AzureResourceException(
                                "Unexpected status code for storage account create", statusCode, errorResponse.Error);
                        },
                        ct);
                }

                await Task.Delay(100, ct);

            } while (createdStorageAccount == null);

            return createdStorageAccount;
        }

        public async Task DeleteStorageAccount(
            string id, string apiVersion = DefaultStorageApiVersion, CancellationToken ct = default)
        {
            // Parse id string to leave just the part from the resource group name and further.
            var urlPart = id.Split('/').Skip(4).Aggregate((acc, part) => acc + '/' + part);

            var requestUri = $"/subscriptions/{_azureOptions.SubscriptionId}/resourceGroups/{urlPart}?api-version={apiVersion}";
            using (var request = new HttpRequestMessage(HttpMethod.Delete, requestUri))
            {
                await Http.SendRequestAndDecodeResponse(
                    Client,
                    request,
                    (statusCode, jsonTextReader) =>
                    {
                        if (statusCode == HttpStatusCode.OK)
                        {
                            Log.LogInformation($"Storage account deleted: {id}");
                        }
                        else if (statusCode == HttpStatusCode.NoContent)
                        {
                            Log.LogInformation($"Storage account did not exist: {id}");
                        }
                        else
                        {
                            var errorResponse = Json.Deserialize<ErrorResponse>(jsonTextReader);
                            throw new AzureResourceException("Unexpected status code for storage account delete", statusCode, errorResponse.Error);
                        }
                    },
                    ct);
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
            {
                return await Http.SendRequestAndDecodeResponse(
                    Client,
                    request,
                    (statusCode, jsonTextReader) =>
                    {
                        if (statusCode == HttpStatusCode.Created)
                        {
                            var roleAssignment = Json.Deserialize<RoleAssignment>(jsonTextReader);
                            return roleAssignment;
                        }

                        // No 200 response, must be an error.
                        var errorResponse = Json.Deserialize<ErrorResponse>(jsonTextReader);
                        throw new AzureResourceException("Unexpected response code for role assignment", statusCode, errorResponse.Error);
                    },
                    ct);
            }
        }

        public async Task<IEnumerable<StorageAccountKey>> GetStorageAccountKeys(string storageAccountId,
            string apiVersion = DefaultStorageApiVersion, CancellationToken ct = default)
        {
            var urlPart = $"{storageAccountId}/listKeys?api-version={apiVersion}";
            using (var request = new HttpRequestMessage(HttpMethod.Post, urlPart))
            {
                return await Http.SendRequestAndDecodeResponse(
                    Client,
                    request,
                    (statusCode, jsonTextReader) =>
                    {
                        if (statusCode == HttpStatusCode.OK)
                        {
                            var storageAccountListKeysResult =
                                Json.Deserialize<StorageAccountListKeysResult>(jsonTextReader);
                            return storageAccountListKeysResult.Keys;
                        }

                        var errorResponse = Json.Deserialize<ErrorResponse>(jsonTextReader);
                        throw new AzureResourceException("Unexpected status code for access keys get", statusCode, errorResponse.Error);
                    },
                    ct);
            }
        }

        private class StorageAccountListKeysResult
        {
            public StorageAccountKey[] Keys { get; private set; }
        }
    }
}
