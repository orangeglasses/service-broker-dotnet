using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace azure.Auth
{
    internal abstract class AzureAuthorizationHandler : DelegatingHandler
    {
        private readonly IConfidentialClientApplication _clientApplication;
        private readonly ILogger<AzureAuthorizationHandler> _log;

        protected AzureAuthorizationHandler(
            IOptions<ConfidentialClientApplicationOptions> azureAuthOptions, ILogger<AzureAuthorizationHandler> log)
        {
            var azureAuth = azureAuthOptions.Value;
            _log = log;

            _clientApplication = ConfidentialClientApplicationBuilder
                .CreateWithApplicationOptions(azureAuth)
                .Build();
        }

        protected abstract IEnumerable<string> Scopes { get; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            // Get authorization token from Azure AD.
            var authenticationResult = await _clientApplication
                .AcquireTokenForClient(Scopes)
                .ExecuteAsync(ct);

            // Set authorization token on request.
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authenticationResult.AccessToken);

            return await base.SendAsync(request, ct);
        }
    }
}
