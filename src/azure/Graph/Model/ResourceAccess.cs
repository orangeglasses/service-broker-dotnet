using System;
using Newtonsoft.Json;

namespace azure.Graph.Model
{
    [JsonObject]
    public class ResourceAccess
    {
        public Guid Id { get; set; }

        public string Type { get; set; }
    }
}