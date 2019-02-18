using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace azure.Storage.Model
{
    [JsonObject]
    public class StorageAccountProperties
    {
        [JsonProperty]
        public StorageAccessTier AccessTier { get; set; }

        [JsonProperty]
        public StorageEncryption Encryption { get; set; }

        [JsonProperty]
        public bool SupportsHttpsTrafficOnly { get; set; }
    }
}
