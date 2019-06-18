using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace azure.Auth
{
    internal class AzureRMAuthorizationHandler : AzureAuthorizationHandler
    {
        public AzureRMAuthorizationHandler(
            IOptions<ConfidentialClientApplicationOptions> azureAuthOptions, ILogger<AzureRMAuthorizationHandler> log)
            : base(azureAuthOptions, log)
        {
        }

        protected override IEnumerable<string> Scopes => new[] { "https://management.core.windows.net/.default" };
    }
}
