using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using azure.ResourceGroups;
using azure.ResourceGroups.Model;
using azure.Storage;
using azure.Storage.Model;
using Microsoft.Extensions.Logging;
using OpenServiceBroker.Instances;

namespace broker.Lib
{
    public class ServiceInstanceBlocking : IServiceInstanceBlocking
    {
        private readonly IAzureResourceGroupClient _azureResourceGroupClient;
        private readonly IAzureStorageProviderClient _azureStorageProviderClient;
        private readonly IAzureStorageClient _azureStorageClient;
        private readonly ILogger<ServiceInstanceBlocking> _log;

        public ServiceInstanceBlocking(
            IAzureResourceGroupClient azureResourceGroupClient,
            IAzureStorageProviderClient azureStorageProviderClient,
            IAzureStorageClient azureStorageClient,
            ILogger<ServiceInstanceBlocking> log)
        {
            _azureResourceGroupClient = azureResourceGroupClient;
            _azureStorageProviderClient = azureStorageProviderClient;
            _azureStorageClient = azureStorageClient;
            _log = log;
        }

        public async Task<ServiceInstanceProvision> ProvisionAsync(ServiceInstanceContext context, ServiceInstanceProvisionRequest request)
        {
            LogContext(_log, "Provision", context);
            LogRequest(_log, request);

            var orgId = request.OrganizationGuid;
            var spaceId = request.SpaceGuid;
            var resourceGroupName = $"{orgId}_{spaceId}";
            var exists = await _azureResourceGroupClient.ResourceGroupExists(resourceGroupName);

            // Create resource group if it does not yet exist.
            if (exists)
            {
                _log.LogInformation($"Resource group {resourceGroupName} exists");
            }
            else
            {
                _log.LogInformation($"Resource group {resourceGroupName} does not exist: creating");

                var resourceGroup = await _azureResourceGroupClient.CreateResourceGroup(new ResourceGroup
                {
                    Name = resourceGroupName,
                    Location = "westeurope",
                    Tags = new Dictionary<string, string>
                    {
                        { "cf_org_id", orgId },
                        { "cf_space_id", spaceId }
                    }
                });
                _log.LogInformation($"Resource group {resourceGroupName} created: {resourceGroup.Id}");
            }

            // Create storage account.
            var storageAccountName = context.InstanceId.Replace("-", "").Substring(0, 24);
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
                        { "cf_org_id", orgId },
                        { "cf_space_id", spaceId },
                        { "cf_service_instance_id", context.InstanceId }
                    }
                });

            return new ServiceInstanceProvision();
        }

        public async Task DeprovisionAsync(ServiceInstanceContext context, string serviceId, string planId)
        {
            LogContext(_log, "Deprovision", context);
            _log.LogInformation($"Deprovision: {{ service_id = {serviceId}, planId = {planId} }}");

            // First retrieve all storage accounts in the subscription because we do not have information here
            // about the resource group of the storage account we wish to delete.
            var storageAccounts = await _azureStorageProviderClient.ListStorageAccounts();

            // Find storage account with the tag containing the service instance id.
            var storageAccount = storageAccounts
                .SingleOrDefault(account => account.Tags
                    .Any(tag => tag.Key == "cf_service_instance_id" && tag.Value == context.InstanceId));
            if (storageAccount != null)
            {
                // Delete storage account based on storage account resource id.
                await _azureStorageClient.DeleteStorageAccount(storageAccount.Id);

                // Parse resource group name from resource id.
                var resourceGroupName = storageAccount.Id.Split('/')[4];
                await _azureResourceGroupClient.DeleteResourceGroupIfEmpty(resourceGroupName);
            }
        }

        public Task<ServiceInstanceResource> FetchAsync(string instanceId)
        {
            throw new System.NotImplementedException();
        }

        public Task UpdateAsync(ServiceInstanceContext context, ServiceInstanceUpdateRequest request)
        {
            throw new System.NotImplementedException();
        }

        private static void LogContext(ILogger log, string operation, ServiceInstanceContext context)
        {
            log.LogInformation(
                $"{operation} - context: {{ instance_id = {context.InstanceId}, " +
                                        $"originating_identity = {{ platform = {context.OriginatingIdentity?.Platform}, " +
                                                                  $"value = {context.OriginatingIdentity?.Value} }} }}");
        }

        private static void LogRequest(ILogger log, ServiceInstanceProvisionRequest request)
        {
            log.LogInformation(
                $"Provision - request: {{ organization_guid = {request.OrganizationGuid}, " +
                                        $"space_guid = {request.SpaceGuid}, " +
                                        $"service_id = {request.ServiceId}, " +
                                        $"plan_id = {request.PlanId}, " +
                                        $"parameters = {request.Parameters}, " +
                                        $"context = {request.Context} }}");
        }
    }
}
