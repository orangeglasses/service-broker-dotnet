using Newtonsoft.Json;

namespace azure.Storage.Model
{
    [JsonObject]
    public class StorageSku
    {
        [JsonProperty]
        public StorageSkuName Name { get; set; }

        [JsonProperty]
        public StorageSkuTier Tier { get; set; }
    }
}
