using System.Threading;
using System.Threading.Tasks;
using OpenServiceBroker.Instances;

namespace operations
{
    public interface IInstanceOps
    {
        Task<ServiceExistence> ServiceExists(
            ServiceInstanceContext context, ServiceInstanceProvisionRequest request, CancellationToken ct = default);

        Task<bool> ServiceExists(ServiceInstanceContext context, string serviceId = null, string planId = null, CancellationToken ct = default);

        (bool started, string operationId) StartProvisioningOperation(ServiceInstanceContext context, ServiceInstanceProvisionRequest request);

        (bool started, string operationId) StartDeprovisioningOperation(ServiceInstanceContext context, string serviceId, string planId);

        OperationProgress GetProvisioningOperationProgress(ServiceInstanceContext context, string serviceId = null, string planId = null, string operation = null);
    }

    public enum ServiceExistence
    {
        DoesNotExist,
        Exists,
        ExistsWithDifferentAttributes
    }
}
