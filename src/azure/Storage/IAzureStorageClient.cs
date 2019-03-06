using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using azure.RoleAssignments.Model;
using azure.Storage.Model;

namespace azure.Storage
{
    public interface IAzureStorageClient
    {
        Task<StorageAccount> CreateStorageAccount(
            string resourceGroupName, StorageAccount storageAccount, string apiVersion = AzureStorageClient.DefaultStorageApiVersion,
            CancellationToken ct = default);

        Task DeleteStorageAccount(
            string id, string apiVersion = AzureStorageClient.DefaultStorageApiVersion, CancellationToken ct = default);

        Task<RoleAssignment> GrantPrincipalAccessToStorageAccount(string storageAccountId,
            Guid roleDefinitionId, Guid principalId,
            string apiVersion = AzureStorageClient.DefaultRoleAssignmentApiVersion, CancellationToken ct = default);

        Task<IEnumerable<StorageAccountKey>> GetStorageAccountKeys(string storageAccountId,
            string apiVersion = AzureStorageClient.DefaultStorageApiVersion, CancellationToken ct = default);
    }
}
