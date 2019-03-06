using System.Collections.Generic;
using Newtonsoft.Json;

namespace azure.Graph.Model
{
    [JsonObject]
    public class ServicePrincipal : DirectoryObject
    {
        public bool AccountEnabled { get; set; }

        public string AppId { get; set; }

        public string DisplayName { get; set; }

        public IList<string> Tags { get; } = new List<string>();
    }
}