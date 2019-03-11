using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using azure.Config;
using azure.Errors;
using azure.Graph;
using azure.Graph.Model;
using azure.Storage;
using broker.Bindings.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using OpenServiceBroker.Bindings;

namespace broker.Bindings
{
    public class ServiceBindingBlocking : IServiceBindingBlocking
    {
        private readonly IAzureStorageProviderClient _azureStorageProviderClient;
        private readonly IAzureStorageClient _azureStorageClient;
        private readonly IMSGraphClient _msGraphClient;
        private readonly AzureAuthOptions _azureAuthOptions;
        private readonly ILogger<ServiceBindingBlocking> _log;

        public ServiceBindingBlocking(
            IAzureStorageProviderClient azureStorageProviderClient,
            IAzureStorageClient azureStorageClient,
            IMSGraphClient msGraphClient,
            IOptions<AzureAuthOptions> azureAuthOptions,
            ILogger<ServiceBindingBlocking> log)
        {
            _azureStorageProviderClient = azureStorageProviderClient;
            _azureStorageClient = azureStorageClient;
            _msGraphClient = msGraphClient;
            _azureAuthOptions = azureAuthOptions.Value;
            _log = log;
        }

        public async Task<ServiceBinding> BindAsync(ServiceBindingContext context, ServiceBindingRequest request)
        {
            LogContext(_log, "Bind", context);
            LogRequest(_log, request);

            // Retrieve Azure Storage account.
            var storageAccounts = await _azureStorageProviderClient.GetStorageAccountsByTag("cf_service_instance_id", context.InstanceId);
            var nrStorageAccounts = storageAccounts.Count();
            if (nrStorageAccounts == 0)
            {
                var message = $"Could not find storage account with tag: cf_service_instance_id = {context.InstanceId}";
                _log.LogWarning(message);
                throw new ArgumentException(message, nameof(context));
            }

            if (nrStorageAccounts > 1)
            {
                var message = $"Found multiple storage accounts for tag: cf_service_instance_id = {context.InstanceId}";
                _log.LogError(message);
                throw new ArgumentException(message, nameof(context));
            }

            var storageAccount = storageAccounts.Single();
            var storageAccountId = storageAccount.Id;

            // Must be in a separate method because Span is not allowed inside async methods.
            string GeneratePassword()
            {
                var randomNumberGenerator = new RNGCryptoServiceProvider();
                var randomNr = new Span<byte>(new byte[16]);
                randomNumberGenerator.GetBytes(randomNr);
                return Convert.ToBase64String(randomNr);
            }

            // Create an Azure AD application.
            var clientSecret = GeneratePassword();
            var application = new Application
            {
                DisplayName = context.BindingId,
                IdentifierUris = { $"https://{context.BindingId}" },
                PasswordCredentials =
                {
                    new PasswordCredential
                    {
                        StartDateTime = DateTimeOffset.UtcNow,
                        EndDateTime = DateTimeOffset.UtcNow.AddYears(2),
                        KeyId = Guid.NewGuid(),
                        SecretText = clientSecret
                    }
                },
                SignInAudience = SignInAudience.AzureADMyOrg,
                Tags = { $"cf_service_id:{request.ServiceId}", $"cf_plan_id:{request.PlanId}", $"cf_binding_id:{context.BindingId}" }
            };
            var createdApplication = await _msGraphClient.CreateApplication(application);

            // Create a service principal for the application in the same tenant.
            var servicePrincipal = new ServicePrincipal
            {
                AccountEnabled = true,
                AppId = createdApplication.AppId,
                DisplayName = createdApplication.DisplayName,
                Tags = { $"cf_service_id:{request.ServiceId}", $"cf_plan_id:{request.PlanId}", $"cf_binding_id:{context.BindingId}" }
            };
            var createdServicePrincipal = await _msGraphClient.CreateServicePrincipal(servicePrincipal);
            var principalId = Guid.Parse(createdServicePrincipal.Id);

            // Assign service principal to roles Storage Blob Data Contributor and Storage Queue Data Contributor.
            var storageBlobDataContributorRoleId = Guid.Parse("ba92f5b4-2d11-453d-a403-e96b0029c9fe");
            await GrantPrincipalAccessToStorageAccount(storageAccountId, storageBlobDataContributorRoleId, principalId);

            var storageQueueDataContributorRoleId = Guid.Parse("974c5e8b-45b9-4653-ba55-5f855dd0fb88");
            await GrantPrincipalAccessToStorageAccount(storageAccountId, storageQueueDataContributorRoleId, principalId);

            // Get the access keys for the storage account.
            var storageAccountKeys = await _azureStorageClient.GetStorageAccountKeys(storageAccountId);

            return new ServiceBinding
            {
                Credentials = JObject.FromObject(new StorageAccountCredentials
                {
                    Urls =
                    {
                        BlobStorageUrl = $"https://{storageAccount.Name}.blob.core.windows.net",
                        QueueStorageUrl = $"https://{storageAccount.Name}.queue.core.windows.net",
                        TableStorageUrl = $"https://{storageAccount.Name}.table.core.windows.net",
                        FileStorageUrl = $"https://{storageAccount.Name}.file.core.windows.net",
                    },
                    SharedKeys = storageAccountKeys
                        .Select(key => new SharedKey
                        {
                            Name = key.KeyName,
                            Permissions = key.Permissions.ToString(),
                            Value = key.Value
                        })
                        .ToArray(),
                    OAuthClientCredentials =
                    {
                        ClientId = createdApplication.AppId,
                        ClientSecret = clientSecret,
                        TokenEndpoint = $"https://login.microsoftonline.com/{_azureAuthOptions.TenantId}/oauth2/v2.0/token",
                        Scopes = new[] { "https://management.core.windows.net/.default" },
                        GrantType = "client_credentials"
                    }
                })
            };
        }

        public async Task UnbindAsync(ServiceBindingContext context, string serviceId, string planId)
        {
            LogContext(_log, "Unbind", context);
            _log.LogInformation($"Deprovision: {{ service_id = {serviceId}, planId = {planId} }}");

            // Delete Azure AD application.
            var bindingId = context.BindingId;
            await _msGraphClient.DeleteApplication(bindingId);
        }

        public Task<ServiceBindingResource> FetchAsync(string instanceId, string bindingId)
        {
            throw new NotImplementedException();
        }

        private async Task GrantPrincipalAccessToStorageAccount(string storageAccountId, Guid roleId, Guid principalId)
        {
            var principalFound = true;
            do
            {
                try
                {
                    await _azureStorageClient.GrantPrincipalAccessToStorageAccount(
                        storageAccountId: storageAccountId, roleDefinitionId: roleId, principalId: principalId);
                    principalFound = true;
                }
                catch (AzureResourceException e)
                {
                    if (e.Error?.Code == "PrincipalNotFound")
                    {
                        principalFound = false;
                    }
                }
            } while (!principalFound);
        }

        private static void LogContext(ILogger log, string operation, ServiceBindingContext context)
        {
            log.LogInformation(
                $"{operation} - context: {{ instance_id = {context.InstanceId}, " +
                                          $"binding_id = {context.BindingId}, " +
                                          $"originating_identity = {{ platform = {context.OriginatingIdentity?.Platform}, " +
                                                                    $"value = {context.OriginatingIdentity?.Value} }} }}");
        }

        private static void LogRequest(ILogger log, ServiceBindingRequest context)
        {
            log.LogInformation(
                $"Bind - request: {{ bind_resource = {{ app_guid = {context.BindResource?.AppGuid}, " +
                                                      $"route = {context.BindResource?.Route} }}, " +
                                   $"context = {context.Context}, " +
                                   $"parameters = {context.Parameters}, " +
                                   $"plan_id = {context.PlanId}, " +
                                   $"service_id = {context.ServiceId} }}");
        }
    }
}
