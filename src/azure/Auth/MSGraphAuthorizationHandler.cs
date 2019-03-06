using System.Collections.Generic;
using azure.Config;
using Microsoft.Extensions.Options;

namespace azure.Auth
{
    internal class MSGraphAuthorizationHandler : AzureAuthorizationHandler
    {
        public MSGraphAuthorizationHandler(IOptions<AzureAuthOptions> azureAuthOptions) : base(azureAuthOptions)
        {
        }

        protected override IEnumerable<string> Scopes => new[] { "https://graph.microsoft.com/.default" };
    }
}
