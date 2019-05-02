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

        public async Task<StorageAccount> GetStorageAccount(
            string resourceGroupName, string storageAccountName, string apiVersion = DefaultStorageApiVersion, CancellationToken ct = default)
        {
            var requestUri =
                $"/subscriptions/{_azureOptions.SubscriptionId}" +
                $"/resourceGroups/{resourceGroupName}" +
                $"/providers/Microsoft.Storage/storageAccounts/{storageAccountName}?api-version={apiVersion}";
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                var storageAccount = await Http.SendRequestAndDecodeResponse(
                    Client,
                    request,
                    (statusCode, jsonTextReader) =>
                    {
                        if (statusCode == HttpStatusCode.OK)
                        {
                            return Json.Deserialize<StorageAccount>(jsonTextReader);
                        }

                        return null;
                    },
                    ct);
                return storageAccount;
            }
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

        public async Task<bool> IsNameAvailable(string storageAccountName, string apiVersion = DefaultStorageApiVersion, CancellationToken ct = default)
        {
            var body = new StorageAccountNameAvailabilityRequestBody(storageAccountName);
            var serializedBody = JsonConvert.SerializeObject(body, Formatting.None, Json.JsonSerializerSettings);

            var requestUri = $"/subscriptions/{_azureOptions.SubscriptionId}/providers/Microsoft.Storage/checkNameAvailability?api-version={apiVersion}";
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(serializedBody, Encoding.UTF8, "application/json")
            };
            using (request)
            {
                return await Http.SendRequestAndDecodeResponse(
                    Client,
                    request,
                    (statusCode, jsonTextReader) =>
                    {
                        if (statusCode == HttpStatusCode.OK)
                        {
                            var responseBody =
                                Json.Deserialize<StorageAccountNameAvailabilityResponseBody>(jsonTextReader);

                            if (responseBody.NameAvailable)
                            {
                                Log.LogDebug($"Storage account name {storageAccountName} is available");
                                return true;
                            }

                            if (!responseBody.NameAvailable &&
                                responseBody.Reason == StorageAccountNameAvailabilityResponseBody.ReasonAlreadyExists)
                            {
                                Log.LogWarning($"Storage account name {storageAccountName} is not available.");
                                return false;
                            }

                            if (!responseBody.NameAvailable &&
                                responseBody.Reason == StorageAccountNameAvailabilityResponseBody.ReasonAccountNameInvalid)
                            {
                                Log.LogError($"Invalid storage account name {storageAccountName} provided: {responseBody.Message}");
                                return false;
                            }

                            throw new AzureResourceException(
                                "Unexpected OK response in storage account name availability check: " +
                                $"nameAvailable = {responseBody.NameAvailable}, " +
                                $"reason = {responseBody.Reason}, " +
                                $"message = {responseBody.Message}",
                                statusCode);
                        }

                        var errorResponse = Json.Deserialize<ErrorResponse>(jsonTextReader);
                        throw new AzureResourceException("Unexpected status code", statusCode, errorResponse.Error);
                    },
                    ct);
            }
        }

        public async Task<IEnumerable<StorageAccount>> ListStorageAccounts(string apiVersion = DefaultStorageApiVersion, CancellationToken ct = default)
        {
            return await InternalListStorageAccounts(apiVersion, ct);
        }

        public async Task<IEnumerable<StorageAccount>> GetStorageAccountsByTag(
            string tagName, string tagValue, string apiVersion = DefaultStorageApiVersion, CancellationToken ct = default)
        {
            var storageAccounts = await InternalListStorageAccounts(apiVersion, ct);
            var taggedStorageAccounts = storageAccounts
                .Where(storageAccount => storageAccount.Tags.TryGetValue(tagName, out var value) &&
                                         string.Equals(value, tagValue, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return taggedStorageAccounts;
        }

        private async Task<IEnumerable<StorageAccount>> InternalListStorageAccounts(string apiVersion, CancellationToken ct)
        {
            var requestUri = $"/subscriptions/{_azureOptions.SubscriptionId}/providers/Microsoft.Storage/storageAccounts?api-version={apiVersion}";
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                return await Http.SendRequestAndDecodeResponse(
                    Client,
                    request,
                    (statusCode, jsonTextReader) =>
                    {
                        if (statusCode == HttpStatusCode.OK)
                        {
                            var storageAccountListResult =
                                Json.Deserialize<StorageAccountListResult>(jsonTextReader);
                            return storageAccountListResult.StorageAccounts;
                        }

                        var errorResponse = Json.Deserialize<ErrorResponse>(jsonTextReader);
                        throw new AzureResourceException("Unexpected error storage accounts list", statusCode,
                            errorResponse.Error);
                    },
                    ct);
            }
        }

        [JsonObject]
        private class StorageAccountListResult
        {
            [JsonProperty("value")]
            public StorageAccount[] StorageAccounts { get; set; }
        }

        [JsonObject]
        private class StorageAccountNameAvailabilityRequestBody
        {
            public StorageAccountNameAvailabilityRequestBody(string name)
            {
                Name = name;
            }

            [JsonProperty]
            public string Name { get; private set; }

            [JsonProperty]
            public string Type => "Microsoft.Storage/storageAccounts";
        }

        [JsonObject]
        private class StorageAccountNameAvailabilityResponseBody
        {
            public const string ReasonAccountNameInvalid = "AccountNameInvalid";
            public const string ReasonAlreadyExists = "AlreadyExists";

            [JsonProperty]
            public bool NameAvailable { get; set; }

            [JsonProperty]
            public string Reason { get; set; }

            [JsonProperty]
            public string Message { get; set; }
        }

        private class StorageAccountListKeysResult
        {
            public StorageAccountKey[] Keys { get; private set; }
        }
    }
}
