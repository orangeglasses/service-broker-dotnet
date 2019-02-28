using System;
using azure.Auth;
using azure.Config;
using azure.ResourceGroups;
using azure.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace azure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAzureServices(
            this IServiceCollection services,
            Action<AzureOptions> configureAzureOptions,
            Action<AzureRMAuthOptions> configureAzureRMAuthOptions)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureAzureOptions == null) throw new ArgumentNullException(nameof(configureAzureOptions));
            if (configureAzureRMAuthOptions == null) throw new ArgumentNullException(nameof(configureAzureRMAuthOptions));

            // Configure Azure and Azure RM options.
            services.Configure(configureAzureOptions);
            services.Configure(configureAzureRMAuthOptions);

            // Add Azure services.
            services.AddTransient<AzureAuthorizationHandler>();
            services
                .AddHttpClient<IAzureResourceGroupClient, AzureResourceGroupClient>((serviceProvider, client) =>
                {
                    var azureOptions = serviceProvider.GetRequiredService<IOptions<AzureOptions>>();
                    var azureConfig = azureOptions.Value;
                    client.BaseAddress =
                        new Uri($"https://management.azure.com/subscriptions/{azureConfig.SubscriptionId}/resourcegroups/");
                })
                .AddHttpMessageHandler<AzureAuthorizationHandler>();

            services
                .AddHttpClient<IAzureStorageProviderClient, AzureStorageProviderClient>((serviceProvider, client) =>
                {
                    var azureOptions = serviceProvider.GetRequiredService<IOptions<AzureOptions>>();
                    var azureConfig = azureOptions.Value;
                    client.BaseAddress =
                        new Uri($"https://management.azure.com/subscriptions/{azureConfig.SubscriptionId}/providers/Microsoft.Storage/");
                })
                .AddHttpMessageHandler<AzureAuthorizationHandler>();

            services
                .AddHttpClient<IAzureStorageClient, AzureStorageClient>((serviceProvider, client) =>
                {
                    var azureOptions = serviceProvider.GetRequiredService<IOptions<AzureOptions>>();
                    var azureConfig = azureOptions.Value;
                    client.BaseAddress =
                        new Uri($"https://management.azure.com/subscriptions/{azureConfig.SubscriptionId}/resourceGroups/");
                })
                .AddHttpMessageHandler<AzureAuthorizationHandler>();

            return services;
        }
    }
}
