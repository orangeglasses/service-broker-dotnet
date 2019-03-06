using System.Collections.Generic;
using azure.Config;
using Microsoft.Extensions.Options;

namespace azure.Auth
{
    internal class AzureRMAuthorizationHandler : AzureAuthorizationHandler
    {
        public AzureRMAuthorizationHandler(IOptions<AzureAuthOptions> azureAuthOptions)
            : base(azureAuthOptions)
        {
        }

        protected override IEnumerable<string> Scopes => new[] { "https://management.core.windows.net/.default" };
    }
}
