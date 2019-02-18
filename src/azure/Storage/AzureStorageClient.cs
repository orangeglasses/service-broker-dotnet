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
    internal class AzureStorageClient : AzureClient, IAzureStorageClient
    {
        public const string DefaultApiVersion = "2018-07-01";

        private readonly HttpClient _client;

        public AzureStorageClient(HttpClient client, ILogger<AzureStorageClient> log)
            : base(log)
        {
            _client = client;
        }

        public async Task<StorageAccount> CreateStorageAccount(
            string resourceGroupName, StorageAccount storageAccount, string apiVersion = DefaultApiVersion, CancellationToken ct = default)
        {
            var serializedStorageAccount =
                JsonConvert.SerializeObject(storageAccount, Formatting.None, JsonSerializerSettings);

            do
            {
                var response = await _client.PutAsync(
                    $"{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccount.Name}?api-version={apiVersion}",
                    new StringContent(serializedStorageAccount, Encoding.UTF8, "application/json"),
                    ct);

                using (response)
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        Log.LogInformation($"Storage account {storageAccount.Name} created from previous request with same properties.");

                        using (var responseStream = await response.Content.ReadAsStreamAsync())
                        using (var streamReader = new StreamReader(responseStream))
                        using (var jsonTextReader = new JsonTextReader(streamReader))
                        {
                            return JsonSerializer.Deserialize<StorageAccount>(jsonTextReader);
                        }
                    }

                    if (response.StatusCode == HttpStatusCode.Accepted)
                    {
                        Log.LogInformation($"Request to create storage account {storageAccount.Name} was accepted.");
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new AzureResourceException("Unexpected status code", response.StatusCode, errorContent);
                    }
                }

                await Task.Delay(100, ct);

            } while (true);
        }
    }
}
