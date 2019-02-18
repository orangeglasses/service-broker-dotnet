using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using azure.Auth;
using azure.Config;
using azure.ResourceGroups;
using azure.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace azure.resources
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAzureServices(
            this IServiceCollection services,
            Action<AzureOptions> configureAzureOptions,
            Action<AzureADAuthOptions> configureAzureADAuthOptions)
        {
            // Configure Azure options.
            var azureOptions = new AzureOptions();
            configureAzureOptions(azureOptions);

            // Configure Azure AD authorization options.
            var azureADAuthOptions = new AzureADAuthOptions();
            configureAzureADAuthOptions(azureADAuthOptions);

            // Register configuration settings.
            services
                .AddSingleton(azureOptions)
                .AddSingleton(azureADAuthOptions);

            // Add Azure services.
            services.AddTransient<AzureAuthorizationHandler>();
            services
                .AddHttpClient<IAzureResourceGroupClient, AzureResourceGroupClient>((serviceProvider, client) =>
                {
                    var azureConfig = serviceProvider.GetRequiredService<AzureOptions>();
                    client.BaseAddress =
                        new Uri($"https://management.azure.com/subscriptions/{azureConfig.SubscriptionId}/resourcegroups/");
                })
                .AddHttpMessageHandler<AzureAuthorizationHandler>();

            services
                .AddHttpClient<IAzureStorageProviderClient, AzureStorageProviderClient>((serviceProvider, client) =>
                {
                    var azureConfig = serviceProvider.GetRequiredService<AzureOptions>();
                    client.BaseAddress =
                        new Uri($"https://management.azure.com/subscriptions/{azureConfig.SubscriptionId}/providers/Microsoft.Storage/");
                })
                .AddHttpMessageHandler<AzureAuthorizationHandler>();

            services
                .AddHttpClient<IAzureStorageClient, AzureStorageClient>((serviceProvider, client) =>
                {
                    var azureConfig = serviceProvider.GetRequiredService<AzureOptions>();
                    client.BaseAddress =
                        new Uri($"https://management.azure.com/subscriptions/{azureConfig.SubscriptionId}/resourceGroups/");
                })
                .AddHttpMessageHandler<AzureAuthorizationHandler>();

            return services;
        }
    }
}
