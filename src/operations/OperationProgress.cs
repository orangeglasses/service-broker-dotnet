using OpenServiceBroker;

namespace operations
{
    public class OperationProgress
    {
        public LastOperationResourceState State { get; set; }

        public string Description { get; set; }
    }
}