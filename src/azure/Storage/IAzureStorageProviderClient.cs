using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using azure.Storage.Model;

namespace azure.Storage
{
    public interface IAzureStorageProviderClient
    {
        Task<bool> IsNameAvailable(
            string storageAccountName, string apiVersion = AzureStorageProviderClient.DefaultApiVersion, CancellationToken ct = default);

        Task<IEnumerable<StorageAccount>> ListStorageAccounts(
            string apiVersion = AzureStorageProviderClient.DefaultApiVersion, CancellationToken ct = default);

        Task<IEnumerable<StorageAccount>> GetStorageAccountsByTag(
            string tagName, string tagValue, string apiVersion = AzureStorageProviderClient.DefaultApiVersion, CancellationToken ct = default);
    }
}
