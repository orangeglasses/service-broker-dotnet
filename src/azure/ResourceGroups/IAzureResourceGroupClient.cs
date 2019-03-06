using System.Threading;
using System.Threading.Tasks;
using azure.ResourceGroups.Model;

namespace azure.ResourceGroups
{
    public interface IAzureResourceGroupClient
    {
        Task<bool> ResourceGroupExists(string name, string apiVersion = AzureResourceGroupClient.DefaultApiVersion, CancellationToken ct = default);

        Task<ResourceGroup> CreateResourceGroup(ResourceGroup resourceGroup, string apiVersion = AzureResourceGroupClient.DefaultApiVersion, CancellationToken ct = default);

        Task<bool> DeleteResourceGroupIfEmpty(string name, string apiVersion = AzureResourceGroupClient.DefaultApiVersion, CancellationToken ct = default);
    }
}
