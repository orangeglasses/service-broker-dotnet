using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenServiceBroker.Instances;

namespace broker.Lib
{
    public class ServiceInstanceBlocking : IServiceInstanceBlocking
    {
        private readonly ILogger<ServiceInstanceBlocking> _log;

        public ServiceInstanceBlocking(ILogger<ServiceInstanceBlocking> log)
        {
            _log = log;
        }

        public Task<ServiceInstanceProvision> ProvisionAsync(ServiceInstanceContext context, ServiceInstanceProvisionRequest request)
        {
            _log.LogInformation(
                $"Provision - context: {{ instance_id = {context.InstanceId}, " +
                                        $"originating_identity = {{ platform = {context.OriginatingIdentity.Platform}, " +
                                                                  $"value = {context.OriginatingIdentity.Value} }} }}");
            _log.LogInformation(
                $"Provision - request: {{ organization_guid = {request.OrganizationGuid}, " +
                                        $"space_guid = {request.SpaceGuid}, " +
                                        $"service_id = {request.ServiceId}, " +
                                        $"plan_id = {request.PlanId}, " +
                                        $"parameters = {request.Parameters}, " +
                                        $"context = {request.Context} }}");

            return Task.FromResult(new ServiceInstanceProvision());
        }

        public Task DeprovisionAsync(ServiceInstanceContext context, string serviceId, string planId)
        {
            _log.LogInformation(
                $"Deprovision - context: {{ instance_id = {context.InstanceId}, " +
                                          $"originating_identity = {{ platform = {context.OriginatingIdentity.Platform}, " +
                                                                    $"value = {context.OriginatingIdentity.Value} }} }}");
            _log.LogInformation($"Deprovision: {{ service_id = {serviceId}, planId = {planId} }}");

            return Task.CompletedTask;
        }

        public Task<ServiceInstanceResource> FetchAsync(string instanceId)
        {
            throw new System.NotImplementedException();
        }

        public Task UpdateAsync(ServiceInstanceContext context, ServiceInstanceUpdateRequest request)
        {
            throw new System.NotImplementedException();
        }
    }
}
