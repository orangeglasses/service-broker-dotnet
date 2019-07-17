using System;
using System.Threading;
using System.Threading.Tasks;
using OpenServiceBroker;
using OpenServiceBroker.Instances;

namespace operations
{
    public abstract class Operation
    {
        public string OperationId { get; }
        public ServiceInstanceContext Context { get; }
        public CancellationTokenSource CancellationTokenSource { get; }

        protected Operation(string operationId, ServiceInstanceContext context)
        {
            OperationId = operationId;
            Context = context;
            CancellationTokenSource = new CancellationTokenSource();
        }

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
    }
}