using Newtonsoft.Json;

namespace azure.Errors
{
    [JsonObject]
    public class ErrorResponse
    {
        [JsonProperty]
        public Error Error { get; private set; }
    }
}
