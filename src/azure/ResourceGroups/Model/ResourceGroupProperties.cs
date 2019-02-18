using Newtonsoft.Json;

namespace azure.ResourceGroups.Model
{
    [JsonObject]
    public class ResourceGroupProperties
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ProvisioningState { get; internal set; }
    }
}
