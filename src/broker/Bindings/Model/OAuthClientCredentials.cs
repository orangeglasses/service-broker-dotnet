using Newtonsoft.Json;

namespace broker.Bindings.Model
{
    public class OAuthClientCredentials
    {
        [JsonProperty("client-id")]
        public string ClientId { get; set; }

        [JsonProperty("client-secret")]
        public string ClientSecret { get; set; }

        [JsonProperty("token-endpoint")]
        public string TokenEndpoint { get; set; }

        [JsonProperty("scopes")]
        public string[] Scopes { get; set; }

        [JsonProperty("grant-type")]
        public string GrantType { get; set; }
    }
}
