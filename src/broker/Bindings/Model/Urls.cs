using Newtonsoft.Json;

namespace broker.Bindings.Model
{
    public class Urls
    {
        [JsonProperty("blob-storage-url")]
        public string BlobStorageUrl { get; set; }

        [JsonProperty("queue-storage-url")]
        public string QueueStorageUrl { get; set; }

        [JsonProperty("table-storage-url")]
        public string TableStorageUrl { get; set; }

        [JsonProperty("file-storage-url")]
        public string FileStorageUrl { get; set; }
    }
}
