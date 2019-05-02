using System.Threading;
using System.Threading.Tasks;
using OpenServiceBroker.Instances;

namespace operations
{
    public interface IInstanceOps
    {
        Task<bool> ServiceExists(
            ServiceInstanceContext context, ServiceInstanceProvisionRequest request, CancellationToken ct = default);

        (bool started, string operationId) StartProvisioningOperation(ServiceInstanceContext context, ServiceInstanceProvisionRequest request);

        ProvisioningOperationProgress GetProvisioningOperationProgress(ServiceInstanceContext context, string serviceId = null, string planId = null, string operation = null);
    }
}
