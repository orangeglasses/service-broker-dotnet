using System;
using System.Threading;
using System.Threading.Tasks;
using OpenServiceBroker;
using OpenServiceBroker.Instances;

namespace operations
{
    public class ProvisioningOperation
    {
        public string OperationId { get; }
        public ServiceInstanceContext Context { get; }
        public ServiceInstanceProvisionRequest Request { get; }
        public CancellationTokenSource CancellationTokenSource { get; }

        internal Func<TaskStatus> ProvisioningOperationTaskStatus { private get; set; }

        internal LastOperationResourceState OperationState
        {
            get
            {
                var taskStatusFunc = ProvisioningOperationTaskStatus;
                if (taskStatusFunc == null)
                {
                    return LastOperationResourceState.InProgress;
                }

                var taskStatus = taskStatusFunc();
                switch (taskStatus)
                {
                    case TaskStatus.Created:
                    case TaskStatus.Running:
                    case TaskStatus.WaitingForActivation:
                    case TaskStatus.WaitingToRun:
                    case TaskStatus.WaitingForChildrenToComplete:
                        return LastOperationResourceState.InProgress;
                    case TaskStatus.RanToCompletion:
                        return LastOperationResourceState.Succeeded;
                    case TaskStatus.Faulted:
                    case TaskStatus.Canceled:
                        return LastOperationResourceState.Failed;
                    default:
                        throw new InvalidOperationException($"Invalid task status {taskStatus}");
                }
            }
        }

        public ProvisioningOperation(string operationId, ServiceInstanceContext context, ServiceInstanceProvisionRequest request)
        {
            OperationId = operationId;
            Context = context;
            Request = request;
            CancellationTokenSource = new CancellationTokenSource();
        }
    }

    public class ProvisioningOperationProgress
    {
        public LastOperationResourceState State { get; set; }

        public string Description { get; set; }
    }
}
