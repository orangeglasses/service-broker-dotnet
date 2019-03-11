using Newtonsoft.Json;

namespace azure.Storage.Model
{
    [JsonObject]
    public class StorageEncryption
    {
        [JsonProperty]
        public StorageEncryptionKeySource KeySource { get; set; }

        [JsonProperty]
        public StorageEncryptionServices Services { get; set; }
    }
}
