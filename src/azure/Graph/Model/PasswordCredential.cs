using System;
using Newtonsoft.Json;

namespace azure.Graph.Model
{
    public class PasswordCredential
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public byte[] CustomKeyIdentifier { get; set; }

        public Guid KeyId { get; set; }

        public DateTimeOffset StartDateTime { get; set; }

        public DateTimeOffset EndDateTime { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string SecretText { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Hint { get; set; }
    }
}