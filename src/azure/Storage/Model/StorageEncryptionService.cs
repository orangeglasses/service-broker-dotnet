using Newtonsoft.Json;

namespace azure.Storage.Model
{
    [JsonObject]
    public class StorageEncryptionService
    {
        [JsonProperty]
        public bool Enabled { get; set; }
    }
}