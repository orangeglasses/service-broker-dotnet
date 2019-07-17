using System.Collections.Generic;
using OpenServiceBroker.Instances;

namespace operations
{
    public class DeprovisioningOperation : Operation
    {
        public string ServiceId { get; }

        public string PlanId { get; }

        public DeprovisioningOperation(string operationId, ServiceInstanceContext context, string serviceId, string planId)
            : base(operationId, context)
        {
            ServiceId = serviceId;
            PlanId = planId;
        }
    }
}
