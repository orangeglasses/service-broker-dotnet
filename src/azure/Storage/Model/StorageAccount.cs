using System.Collections.Generic;
using Newtonsoft.Json;

namespace azure.Storage.Model
{
    [JsonObject]
    public class StorageAccount
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Id { get; private set; }

        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public StorageKind Kind { get; set; }

        [JsonProperty]
        public string Location { get; set; }

        [JsonProperty]
        public StorageAccountProperties Properties { get; set; }

        [JsonProperty]
        public StorageSku Sku { get; set; }

        [JsonProperty]
        public IDictionary<string, string> Tags { get; set; }

        private bool ShouldSerializeName() => false;
    }
}
