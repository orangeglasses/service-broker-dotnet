using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenServiceBroker.Instances;

namespace operations
{
    public abstract class InstanceOps : IInstanceOps
    {
        private readonly ILogger<InstanceOps> _log;
        private ImmutableDictionary<ProvisioningOperation, string> _provisioningOperations;
        private ImmutableDictionary<DeprovisioningOperation, string> _deprovisioningOperations;

        protected InstanceOps(
            ProvisioningOpEquality provisioningOpEquality, DeprovisioningOpEquality deprovisioningOpEquality,
            ILogger<InstanceOps> log)
        {
            _log = log;
            _provisioningOperations =
                ImmutableDictionary.CreateBuilder<ProvisioningOperation, string>(provisioningOpEquality).ToImmutable();
            _deprovisioningOperations =
                ImmutableDictionary.CreateBuilder<DeprovisioningOperation, string>(deprovisioningOpEquality).ToImmutable();
        }

        public abstract Task<ServiceExistence> ServiceExists(
            ServiceInstanceContext context, ServiceInstanceProvisionRequest request, CancellationToken ct = default);

        public abstract Task<bool> ServiceExists(
            ServiceInstanceContext context, string serviceId = null, string planId = null, CancellationToken ct = default);

        protected abstract Task StartProvisioningOperation(string operationId, ServiceInstanceContext context,
            ServiceInstanceProvisionRequest request, CancellationToken ct = default);

        protected abstract Task StartDeprovisioningOperation(string operationId, ServiceInstanceContext context,
            string serviceId, string planId, CancellationToken ct = default);

        public (bool started, string operationId) StartProvisioningOperation(ServiceInstanceContext context, ServiceInstanceProvisionRequest request)
        {
            LogContext(_log.LogInformation, "Provision", context);
            LogRequest(_log.LogInformation, request);

            var newOperationId = Guid.NewGuid().ToString();

            // Add a new provisioning operation to the list if a provisioning operation does not yet exist according
            // to the provided equality comparer. In this case, return the new operation id. If the operation already
            // exists, return the existing operation id.
            var provisioningOperation = new ProvisioningOperation(newOperationId, context, request);

            return StartOperation(
                provisioningOperation,
                _provisioningOperations,
                (operation, ct) => StartProvisioningOperation(operation.OperationId, operation.Context, operation.Request, ct));
        }

        public (bool started, string operationId) StartDeprovisioningOperation(ServiceInstanceContext context, string serviceId, string planId)
        {
            LogContext(_log.LogInformation, "Deprovision", context);
            _log.LogInformation($"Deprovision: {{ service_id = {serviceId}, plan_id = {planId} }}");

            var newOperationId = Guid.NewGuid().ToString();

            // Add a new deprovisioning operation to the list if a deprovisioning operation does not yet exist according
            // to the provided equality comparer. In this case, return the new operation id. If the operation already
            // exists, return the existing operation id.
            var deprovisioningOperation = new DeprovisioningOperation(newOperationId, context, serviceId, planId);

            return StartOperation(
                deprovisioningOperation,
                _deprovisioningOperations,
                (operation, ct) => StartDeprovisioningOperation(operation.OperationId, operation.Context, operation.ServiceId, operation.PlanId, ct));
        }

        private static (bool started, string operationId) StartOperation<T>(
            T operation, ImmutableDictionary<T, string> operations, Func<T, CancellationToken, Task> innerOp)
            where T : Operation
        {
            // Add a new (de)provisioning operation to the list if a (de)provisioning operation does not yet exist according
            // to the provided equality comparer. In this case, return the new operation id. If the operation already
            // exists, return the existing operation id.
            var newOperationId = operation.OperationId;
            var currentOperationId = ImmutableInterlocked.AddOrUpdate(
                ref operations,
                operation,
                newOperationId,
                (op, existingOperationId) => existingOperationId);

            // We now know whether a new operation was added or an attempt was made to start a provisioning operation
            // with the same parameters as a running operation.
            if (currentOperationId == newOperationId)
            {
                // This is a new provisioning operation so we need to start the provisioning process.
                var cancellationToken = operation.CancellationTokenSource.Token;

                Task.Factory.StartNew(
                    async () =>
                    {
                        var op = operation;
                        var operationTask = innerOp(op, cancellationToken);
                        op.ProvisioningOperationTaskStatus = () => operationTask.Status;

                        // Always remove the (de)provisioning operation from the list of running operations.
                        await operationTask
                            // ReSharper disable once MethodSupportsCancellation
                            .ContinueWith(async _ =>
                            {
                                // ReSharper disable once MethodSupportsCancellation
                                await Task.Delay(TimeSpan.FromSeconds(60)).ConfigureAwait(false);

                                // Clean up (de)provisioning operation.
                                ImmutableInterlocked.TryRemove(ref operations, op, out var _);
                            })
                            .Unwrap()
                            .ConfigureAwait(false);
                    },
                    cancellationToken,
                    TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness | TaskCreationOptions.HideScheduler,
                    TaskScheduler.Default);

                return (started: true, operationId: newOperationId);
            }

            // This is an attempt to provision a service instance that is already in the process of being provisioned.
            return (started: false, operationId: currentOperationId);
        }

        public OperationProgress GetProvisioningOperationProgress(
            ServiceInstanceContext context, string serviceId = null, string planId = null, string operation = null)
        {
            var instanceId = context.InstanceId;

            // Attempt to get provisioning operation by instance id.
            var runningProvisioningOperation = _provisioningOperations.Keys.SingleOrDefault(op => op.Context.InstanceId == instanceId);
            if (runningProvisioningOperation == null)
            {
                throw new InvalidOperationException(
                    $"Can not find provisioning operation for instance id {instanceId}");
            }

            if (_provisioningOperations.TryGetValue(runningProvisioningOperation, out var runningOperationId))
            {
                // Check that operation matches passed in operation id.
                if (!string.Equals(operation, runningOperationId))
                {
                    throw new ArgumentException(
                        $"Provided instance id {instanceId} and found operation id {runningOperationId} " +
                        $"do not match provided operation id {operation}");
                }

                // Found provisioning operation: return status.
                return new OperationProgress
                {
                    State = runningProvisioningOperation.OperationState
                };
            }

            throw new InvalidOperationException(
                "Calling progress for provisioning operation that has already succeeded or failed is not supported");
        }

        private delegate void Logger(string message, params object[] args);

        private static void LogContext(Logger log, string operation, ServiceInstanceContext context)
        {
            log($"{operation} - context: {{ instance_id = {context.InstanceId}, " +
                $"originating_identity = {{ platform = {context.OriginatingIdentity?.Platform}, " +
                $"value = {context.OriginatingIdentity?.Value} }} }}");
        }

        private static void LogRequest(Logger log, ServiceInstanceProvisionRequest request)
        {
            log($"Provision - request: {{ organization_guid = {request.OrganizationGuid}, " +
                $"space_guid = {request.SpaceGuid}, " +
                $"service_id = {request.ServiceId}, " +
                $"plan_id = {request.PlanId}, " +
                $"parameters = {request.Parameters}, " +
                $"context = {request.Context} }}");
        }
    }
}
