using Newtonsoft.Json;

namespace broker.Bindings.Model
{
    public class StorageAccountCredentials
    {
        [JsonProperty("shared-keys")]
        public SharedKey[] SharedKeys { get; set; }

        [JsonProperty("oauth-client-credentials")]
        public OAuthClientCredentials OAuthClientCredentials { get; } = new OAuthClientCredentials();

        [JsonProperty("urls")]
        public Urls Urls { get; private set; } = new Urls();
    }
}
