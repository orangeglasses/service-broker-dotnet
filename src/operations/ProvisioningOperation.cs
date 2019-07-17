using OpenServiceBroker.Instances;

namespace operations
{
    public class ProvisioningOperation : Operation
    {
        public ServiceInstanceProvisionRequest Request { get; }

        public ProvisioningOperation(string operationId, ServiceInstanceContext context, ServiceInstanceProvisionRequest request)
            : base(operationId, context)
        {
            Request = request;
        }
    }
}
