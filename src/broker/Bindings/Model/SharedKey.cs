using Newtonsoft.Json;

namespace broker.Bindings.Model
{
    public class SharedKey
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("permissions")]
        public string Permissions { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}