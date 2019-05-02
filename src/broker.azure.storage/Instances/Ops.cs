using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using azure.ResourceGroups;
using azure.ResourceGroups.Model;
using azure.Storage;
using azure.Storage.Model;
using Microsoft.Extensions.Logging;
using operations;
using OpenServiceBroker.Instances;

namespace broker.azure.storage.Instances
{
    public class Ops : InstanceOps
    {
        private readonly IAzureResourceGroupClient _azureResourceGroupClient;
        private readonly IAzureStorageClient _azureStorageClient;
        private readonly ILogger<Ops> _log;

        public Ops(IAzureResourceGroupClient azureResourceGroupClient, IAzureStorageClient azureStorageClient, OpsEquality opsEquality, ILogger<Ops> log)
            : base(opsEquality, log)
        {
            _azureResourceGroupClient = azureResourceGroupClient;
            _azureStorageClient = azureStorageClient;
            _log = log;
        }

        public override async Task<bool> ServiceExists(
            ServiceInstanceContext context, ServiceInstanceProvisionRequest request, CancellationToken ct = default)
        {
            // Check if resource group exists.
            var orgId = request.OrganizationGuid;
            var spaceId = request.SpaceGuid;
            var resourceGroupName = $"{orgId}_{spaceId}";
            var resourceGroupExists = await _azureResourceGroupClient.ResourceGroupExists(resourceGroupName, ct: ct);

            if (!resourceGroupExists)
            {
                return false;
            }

            // Resource group exists: check if storage account exists in resource group.
            var storageAccountName = context.InstanceId.Replace("-", "").Substring(0, 24);
            var storageAccount = await _azureStorageClient.GetStorageAccount(resourceGroupName, storageAccountName, ct: ct);
            var storageAccountExists = storageAccount != null;
            return storageAccountExists;
        }

        protected override async Task StartProvisioningOperation(
            string operationId, ServiceInstanceContext context, ServiceInstanceProvisionRequest request, CancellationToken ct = default)
        {
            // Create resource group if it does not yet exist.
            var orgId = request.OrganizationGuid;
            var spaceId = request.SpaceGuid;
            var resourceGroupName = $"{orgId}_{spaceId}";
            await CreateResourceGroupIfNotExists(resourceGroupName, orgId, spaceId, ct);

            // Create storage account.
            var storageAccountName = context.InstanceId.Replace("-", "").Substring(0, 24);
            await CreateStorageAccount(resourceGroupName, storageAccountName, context, request, ct);
        }

        private async Task CreateStorageAccount(
            string resourceGroupName, string storageAccountName, ServiceInstanceContext context, ServiceInstanceProvisionRequest request,
            CancellationToken ct)
        {
            await _azureStorageClient.CreateStorageAccount(
                resourceGroupName,
                new StorageAccount
                {
                    Name = storageAccountName,
                    Kind = StorageKind.StorageV2,
                    Location = "westeurope",
                    Properties = new StorageAccountProperties
                    {
                        AccessTier = StorageAccessTier.Hot,
                        Encryption = new StorageEncryption
                        {
                            KeySource = StorageEncryptionKeySource.Storage,
                            Services = new StorageEncryptionServices
                            {
                                Blob = new StorageEncryptionService { Enabled = true },
                                File = new StorageEncryptionService { Enabled = true },
                                Table = new StorageEncryptionService { Enabled = true },
                                Queue = new StorageEncryptionService { Enabled = true }
                            }
                        },
                        SupportsHttpsTrafficOnly = true
                    },
                    Sku = new StorageSku
                    {
                        Name = StorageSkuName.Standard_LRS,
                        Tier = StorageSkuTier.Standard
                    },
                    Tags = new Dictionary<string, string>
                    {
                        { "cf_org_id", request.OrganizationGuid },
                        { "cf_space_id", request.SpaceGuid },
                        { "cf_service_id", request.ServiceId },
                        { "cf_plan_id", request.PlanId },
                        { "cf_service_instance_id", context.InstanceId }
                    }
                },
                ct: ct);
        }

        private async Task CreateResourceGroupIfNotExists(string resourceGroupName, string orgId, string spaceId, CancellationToken ct)
        {
            // Create resource group if it doesn't exist yet.
            var exists = await _azureResourceGroupClient.ResourceGroupExists(resourceGroupName, ct: ct);
            if (exists)
            {
                _log.LogInformation($"Resource group {resourceGroupName} exists");
            }
            else
            {
                _log.LogInformation($"Resource group {resourceGroupName} does not exist: creating");

                var resourceGroup = await _azureResourceGroupClient.CreateResourceGroup(
                    new ResourceGroup
                    {
                        Name = resourceGroupName,
                        Location = "westeurope",
                        Tags = new Dictionary<string, string>
                        {
                            { "cf_org_id", orgId },
                            { "cf_space_id", spaceId }
                        }
                    },
                    ct: ct);

                _log.LogInformation($"Resource group {resourceGroupName} created: {resourceGroup.Id}");
            }
        }
    }
}
