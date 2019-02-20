using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using azure.Config;
using Microsoft.Identity.Client;

namespace azure.Auth
{
    internal class AzureAuthorizationHandler : DelegatingHandler
    {
        private static readonly TokenCache AppTokenCache = new TokenCache();

        private readonly IConfidentialClientApplication _clientApplication;

        public AzureAuthorizationHandler(AzureADAuthOptions azureADAuthOptions)
        {
            _clientApplication = new ConfidentialClientApplication(
                azureADAuthOptions.ClientId,
                $"{azureADAuthOptions.Instance}{azureADAuthOptions.TenantId}",
                "http://www.nu.nl",
                new ClientCredential(azureADAuthOptions.ClientSecret),
                null,
                AppTokenCache);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            // Get authorization token from Azure AD.
            var authenticationResult =
                await _clientApplication.AcquireTokenForClientAsync(new[] { "https://management.core.windows.net/.default" });

            // Set authorization token on request.
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authenticationResult.AccessToken);

            return await base.SendAsync(request, ct);
        }
    }
}
