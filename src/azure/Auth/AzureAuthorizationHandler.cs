using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using azure.Config;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace azure.Auth
{
    internal abstract class AzureAuthorizationHandler : DelegatingHandler
    {
        private static readonly TokenCache AppTokenCache = new TokenCache();

        private readonly IConfidentialClientApplication _clientApplication;

        protected AzureAuthorizationHandler(IOptions<AzureAuthOptions> azureAuthOptions)
        {
            var azureAuth = azureAuthOptions.Value;
            _clientApplication = new ConfidentialClientApplication(
                azureAuth.ClientId,
                $"{azureAuth.Instance}{azureAuth.TenantId}",
                $"https://{azureAuth.ClientId}",
                new ClientCredential(azureAuth.ClientSecret),
                null,
                AppTokenCache);
        }

        protected abstract IEnumerable<string> Scopes { get; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            // Get authorization token from Azure AD.
            var authenticationResult = await _clientApplication.AcquireTokenForClientAsync(Scopes);

            // Set authorization token on request.
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authenticationResult.AccessToken);

            return await base.SendAsync(request, ct);
        }
    }
}