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

        protected InstanceOps(OpsEquality opsEquality, ILogger<InstanceOps> log)
        {
            _log = log;
            var builder = ImmutableDictionary.CreateBuilder<ProvisioningOperation, string>(opsEquality);
            _provisioningOperations = builder.ToImmutable();
        }

        public abstract Task<bool> ServiceExists(ServiceInstanceContext context, ServiceInstanceProvisionRequest request, CancellationToken ct = default);

        protected abstract Task StartProvisioningOperation(string operationId, ServiceInstanceContext context,
            ServiceInstanceProvisionRequest request, CancellationToken ct = default);

        public (bool started, string operationId) StartProvisioningOperation(ServiceInstanceContext context, ServiceInstanceProvisionRequest request)
        {
            LogContext(_log.LogInformation, "Provision", context);
            LogRequest(_log.LogInformation, request);

            var newOperationId = Guid.NewGuid().ToString();

            // Add a new provisioning operation to the list if a provisioning operation does not yet exist according
            // to the provided equality comparer. In this case, return the new operation id. If the operation already
            // exists, return the existing operation id.
            var provisioningOperation = new ProvisioningOperation(newOperationId, context, request);
            var currentOperationId = ImmutableInterlocked.AddOrUpdate(
                ref _provisioningOperations,
                provisioningOperation,
                newOperationId,
                (operation, existingOperationId) => existingOperationId);

            // We now know whether a new operation was added or an attempt was made to start a provisioning operation
            // with the same parameters as a running operation.
            if (currentOperationId == newOperationId)
            {
                // This is a new provisioning operation so we need to start the provisioning process.
                var cancellationToken = provisioningOperation.CancellationTokenSource.Token;
                Task.Factory.StartNew(
                    async () =>
                    {
                        var op = provisioningOperation;
                        var newOpId = newOperationId;
                        var opContext = context;
                        var opRequest = request;

                        var provisioningOperationTask = StartProvisioningOperation(newOpId, opContext, opRequest, cancellationToken);
                        op.ProvisioningOperationTaskStatus = () => provisioningOperationTask.Status;

                        // Always remove the provisioning operation from the list of running operations.
                        await provisioningOperationTask
                            // ReSharper disable once MethodSupportsCancellation
                            .ContinueWith(async _ =>
                            {
                                // ReSharper disable once MethodSupportsCancellation
                                await Task.Delay(TimeSpan.FromSeconds(60)).ConfigureAwait(false);

                                // Clean up provisioning operation.
                                ImmutableInterlocked.TryRemove(ref _provisioningOperations, op, out var _);
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

        public ProvisioningOperationProgress GetProvisioningOperationProgress(
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
                return new ProvisioningOperationProgress
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
