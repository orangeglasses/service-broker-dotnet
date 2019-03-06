using Newtonsoft.Json;

namespace azure.Graph.Model
{
    [JsonObject]
    public class DirectoryObject
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Id { get; private set; }
    }
}
