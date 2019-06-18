using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace azure.Auth
{
    internal class MSGraphAuthorizationHandler : AzureAuthorizationHandler
    {
        public MSGraphAuthorizationHandler(
            IOptions<ConfidentialClientApplicationOptions> azureAuthOptions, ILogger<MSGraphAuthorizationHandler> log)
            : base(azureAuthOptions, log)
        {
        }

        protected override IEnumerable<string> Scopes => new[] { "https://graph.microsoft.com/.default" };
    }
}
