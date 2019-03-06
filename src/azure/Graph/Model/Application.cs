using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace azure.Graph.Model
{
    [JsonObject]
    public class Application : DirectoryObject
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AppId { get; private set; }

        public string DisplayName { get; set; }

        public IList<string> IdentifierUris { get; } = new List<string>();

        public IList<PasswordCredential> PasswordCredentials { get; } = new List<PasswordCredential>();

        public IList<RequiredResourceAccess> RequiredResourceAccess { get; } = new List<RequiredResourceAccess>();

        [JsonConverter(typeof(StringEnumConverter))]
        public SignInAudience SignInAudience { get; set; }

        public IList<string> Tags { get; } = new List<string>();
    }
}
