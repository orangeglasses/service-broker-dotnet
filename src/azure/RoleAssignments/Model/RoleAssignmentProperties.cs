using System;
using Newtonsoft.Json;

namespace azure.RoleAssignments.Model
{
    public class RoleAssignmentProperties
    {
        [JsonProperty]
        public string RoleDefinitionId { get; set; }

        [JsonProperty]
        public Guid PrincipalId { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PrincipalType { get; private set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Scope { get; private set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTimeOffset CreatedOn { get; private set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTimeOffset UpdatedOn { get; private set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Guid? CreatedBy { get; private set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Guid? UpdatedBy { get; private set; }
    }
}