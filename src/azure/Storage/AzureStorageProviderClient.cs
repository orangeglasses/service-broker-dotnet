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
using azure.Lib;
using azure.Storage.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace azure.Storage
{
    internal class AzureStorageProviderClient : AzureClient, IAzureStorageProviderClient
    {
        public const string DefaultApiVersion = "2018-07-01";

        private readonly AzureOptions _azureOptions;

        public AzureStorageProviderClient(
            HttpClient client, IHttp http, IJson json, IOptions<AzureOptions> azureOptions, ILogger<AzureStorageProviderClient> log)
            : base(client, http, json, log)
        {
            _azureOptions = azureOptions.Value;
        }

        public async Task<bool> IsNameAvailable(string storageAccountName, string apiVersion = DefaultApiVersion, CancellationToken ct = default)
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

        public async Task<IEnumerable<StorageAccount>> ListStorageAccounts(string apiVersion = DefaultApiVersion, CancellationToken ct = default)
        {
            return await InternalListStorageAccounts(apiVersion, ct);
        }

        public async Task<IEnumerable<StorageAccount>> GetStorageAccountsByTag(
            string tagName, string tagValue, string apiVersion = DefaultApiVersion, CancellationToken ct = default)
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
    }
}
