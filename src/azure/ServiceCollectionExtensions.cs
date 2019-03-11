using System;
using azure.Auth;
using azure.Config;
using azure.Graph;
using azure.Lib;
using azure.ResourceGroups;
using azure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace azure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAzureServices(
            this IServiceCollection services,
            Action<AzureOptions> configureAzureOptions,
            Action<AzureAuthOptions> configureAzureAuthOptions)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureAzureOptions == null) throw new ArgumentNullException(nameof(configureAzureOptions));
            if (configureAzureAuthOptions == null) throw new ArgumentNullException(nameof(configureAzureAuthOptions));

            // Configure Azure and Azure RM options.
            services.Configure(configureAzureOptions);
            services.Configure(configureAzureAuthOptions);

            // Add http services.
            services.AddSingleton<IJson, Json>();
            services.AddSingleton<IHttp, Http>();

            // Add Azure RM services.
            services.AddTransient<AzureRMAuthorizationHandler>();
            services
                .AddHttpClient<IAzureResourceGroupClient, AzureResourceGroupClient>((serviceProvider, client) =>
                {
                    client.BaseAddress = new Uri($"https://management.azure.com");
                })
                .AddHttpMessageHandler<AzureRMAuthorizationHandler>();

            services
                .AddHttpClient<IAzureStorageProviderClient, AzureStorageProviderClient>((serviceProvider, client) =>
                {
                    client.BaseAddress = new Uri($"https://management.azure.com");
                })
                .AddHttpMessageHandler<AzureRMAuthorizationHandler>();

            services
                .AddHttpClient<IAzureStorageClient, AzureStorageClient>((serviceProvider, client) =>
                {
                    client.BaseAddress = new Uri($"https://management.azure.com");
                })
                .AddHttpMessageHandler<AzureRMAuthorizationHandler>();

            // Add Microsoft Graph services.
            services.AddTransient<MSGraphAuthorizationHandler>();
            services
                .AddHttpClient<IMSGraphClient, MSGraphClient>(client =>
                {
                    client.BaseAddress = new Uri("https://graph.microsoft.com/beta/");
                })
                .AddHttpMessageHandler<MSGraphAuthorizationHandler>();

            return services;
        }
    }
}
