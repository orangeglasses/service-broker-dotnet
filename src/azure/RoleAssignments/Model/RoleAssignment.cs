using Newtonsoft.Json;

namespace azure.RoleAssignments.Model
{
    [JsonObject]
    public class RoleAssignment
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Id { get; private set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Type { get; private set; }
    }
}