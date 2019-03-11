using System.Collections.Generic;
using Newtonsoft.Json;

namespace azure.ResourceGroups.Model
{
    [JsonObject]
    public class ResourceGroup
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Id { get; internal set; }

        [JsonProperty]
        public string Location { get; set; }

        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ResourceGroupProperties Properties { get; internal set; }

        [JsonProperty]
        public IDictionary<string, string> Tags { get; set; }

        private bool ShouldSerializeName() => false;
    }
}
