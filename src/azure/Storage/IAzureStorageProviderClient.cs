using System.Threading;
using System.Threading.Tasks;

namespace azure.Storage
{
    public interface IAzureStorageProviderClient
    {
        Task<bool> IsNameAvailable(
            string storageAccountName, string apiVersion = AzureStorageProviderClient.DefaultApiVersion, CancellationToken ct = default);
    }
}
