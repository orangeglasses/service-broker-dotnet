using System.Collections.Generic;
using Newtonsoft.Json;

namespace azure.Graph.Model
{
    [JsonObject]
    public class RequiredResourceAccess
    {
        public string ResourceAppId { get; set; }

        public IList<ResourceAccess> ResourceAccess { get; set; }
    }
}