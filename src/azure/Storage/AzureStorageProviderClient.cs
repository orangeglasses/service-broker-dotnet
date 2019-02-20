using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using azure.resources;
using azure.Storage.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace azure.Storage
{
    internal class AzureStorageProviderClient : AzureClient, IAzureStorageProviderClient
    {
        public const string DefaultApiVersion = "2018-07-01";

        public AzureStorageProviderClient(HttpClient client, ILogger<AzureStorageProviderClient> log)
            : base(client, log)
        {
        }

        public async Task<bool> IsNameAvailable(string storageAccountName, string apiVersion = DefaultApiVersion, CancellationToken ct = default)
        {
            var body = new StorageAccountNameAvailabilityRequestBody(storageAccountName);
            var serializedBody = JsonConvert.SerializeObject(body, Formatting.None, JsonSerializerSettings);

            var response = await Client.PostAsync(
                $"checkNameAvailability?api-version={apiVersion}",
                new StringContent(serializedBody, Encoding.UTF8, "application/json"),
                ct);
            using (response)
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (var responseStream = await response.Content.ReadAsStreamAsync())
                    using (var streamReader = new StreamReader(responseStream))
                    using (var jsonTextReader = new JsonTextReader(streamReader))
                    {
                        var responseBody =
                            JsonSerializer.Deserialize<StorageAccountNameAvailabilityResponseBody>(jsonTextReader);

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
                            response.StatusCode);
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                throw new AzureResourceException("Unexpected status code", response.StatusCode, errorContent);
            }
        }

        public async Task<IEnumerable<StorageAccount>> ListStorageAccounts(string apiVersion = DefaultApiVersion, CancellationToken ct = default)
        {
            var response = await Client.GetAsync($"storageAccounts?api-version={apiVersion}", ct);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                using (var responseStream = await response.Content.ReadAsStreamAsync())
                using (var streamReader = new StreamReader(responseStream))
                using (var jsonTextReader = new JsonTextReader(streamReader))
                {
                    var storageAccountListResult =
                        JsonSerializer.Deserialize<StorageAccountListResult>(jsonTextReader);
                    return storageAccountListResult.StorageAccounts;
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new AzureResourceException("Unexpected error code", response.StatusCode, errorContent);
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
