using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using operations;
using OpenServiceBroker;
using OpenServiceBroker.Instances;

namespace broker.Instances
{
    public class ServiceInstanceDeferred : IServiceInstanceDeferred
    {
        private readonly IInstanceOps _instanceOps;
        private readonly ILogger<ServiceInstanceDeferred> _log;

        public ServiceInstanceDeferred(IInstanceOps instanceOps, ILogger<ServiceInstanceDeferred> log)
        {
            _instanceOps = instanceOps;
            _log = log;
        }

        public async Task<ServiceInstanceAsyncOperation> ProvisionAsync(ServiceInstanceContext context, ServiceInstanceProvisionRequest request)
        {
            // Check whether a service already exists with the exact same parameters. If so, return
            // unchanged (will result in 200 OK).
            if (await _instanceOps.ServiceExists(context, request))
            {
                return new ServiceInstanceAsyncOperation
                {
                    Completed = true,
                    Result = new ServiceInstanceProvision
                    {
                        Unchanged = true
                    }
                };
            }

            var (started, operationId) = _instanceOps.StartProvisioningOperation(context, request);

            if (started)
            {
                // Provisioning has started.
                return new ServiceInstanceAsyncOperation
                {
                    Completed = false,
                    Operation = operationId,
                    Result = new ServiceInstanceProvision
                    {
                        Unchanged = false
                    }
                };
            }

            // Operation with the same parameters was already in progress: return existing operation id.
            return new ServiceInstanceAsyncOperation
            {
                Operation = operationId
            };
        }

        public async Task<ServiceInstanceAsyncOperation> UpdateAsync(ServiceInstanceContext context, ServiceInstanceUpdateRequest request)
        {
            throw new NotImplementedException();
        }

        public async Task<AsyncOperation> DeprovisionAsync(ServiceInstanceContext context, string serviceId = null, string planId = null)
        {
            throw new NotImplementedException();
        }

        public async Task<ServiceInstanceResource> FetchAsync(string instanceId)
        {
            throw new NotImplementedException();
        }

        public async Task<LastOperationResource> GetLastOperationAsync(
            ServiceInstanceContext context, string serviceId = null, string planId = null, string operation = null)
        {
            var provisioningOperationProgress = _instanceOps.GetProvisioningOperationProgress(context, serviceId, planId, operation);
            return await Task.FromResult(new LastOperationResource
            {
                State = provisioningOperationProgress.State
            });
        }
    }
}
