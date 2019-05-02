using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using operations;
using OpenServiceBroker.Instances;
using Xunit;

namespace broker.Instances
{
    public class ServiceInstanceDeferredTests
    {
        [Fact]
        public async Task Provision_ServiceExists()
        {
            var serviceInstanceContext = new ServiceInstanceContext("instance_id");
            var serviceInstanceProvisionRequest = new ServiceInstanceProvisionRequest();

            // Configure mock instance ops.
            var instanceOps = new Mock<IInstanceOps>(MockBehavior.Strict);
            instanceOps
                .Setup(ops => ops.ServiceExists(serviceInstanceContext, serviceInstanceProvisionRequest, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Configure mock logger.
            var logger = new Mock<ILogger<ServiceInstanceDeferred>>(MockBehavior.Loose);

            // Call method to test.
            var serviceInstanceDeferred = new ServiceInstanceDeferred(instanceOps.Object, logger.Object);
            var serviceInstanceAsyncOperation =
                await serviceInstanceDeferred.ProvisionAsync(serviceInstanceContext, serviceInstanceProvisionRequest);

            Assert.NotNull(serviceInstanceAsyncOperation);
            Assert.True(serviceInstanceAsyncOperation.Completed);
            Assert.True(serviceInstanceAsyncOperation.Result.Unchanged);
        }
    }
}
