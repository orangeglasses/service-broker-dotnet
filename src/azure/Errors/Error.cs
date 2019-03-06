using Newtonsoft.Json;

namespace azure.Errors
{
    [JsonObject]
    public class Error
    {
        [JsonProperty]
        public string Code { get; private set; }

        [JsonProperty]
        public string Message { get; private set; }
    }
}
