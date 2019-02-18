using Newtonsoft.Json;

namespace azure.Storage.Model
{
    [JsonObject]
    public class StorageEncryptionServices
    {
        [JsonProperty]
        public StorageEncryptionService Blob { get; set; }

        [JsonProperty]
        public StorageEncryptionService File { get; set; }

        [JsonProperty]
        public StorageEncryptionService Queue { get; set; }

        [JsonProperty]
        public StorageEncryptionService Table { get; set; }
    }
}