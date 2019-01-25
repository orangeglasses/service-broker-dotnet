using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OpenServiceBroker.Bindings;

namespace broker.Lib
{
    public class ServiceBindingBlocking : IServiceBindingBlocking
    {
        private readonly ILogger<ServiceBindingBlocking> _log;

        public ServiceBindingBlocking(ILogger<ServiceBindingBlocking> log)
        {
            _log = log;
        }

        public Task<ServiceBinding> BindAsync(ServiceBindingContext context, ServiceBindingRequest request)
        {
            LogContext(_log, "Bind", context);
            LogRequest(_log, request);

            return Task.FromResult(new ServiceBinding
            {
                Credentials = JObject.FromObject(new
                {
                    connectionString = "<very secret connection string>"
                })
            });
        }

        public Task UnbindAsync(ServiceBindingContext context, string serviceId, string planId)
        {
            LogContext(_log, "Unbind", context);
            _log.LogInformation($"Deprovision: {{ service_id = {serviceId}, planId = {planId} }}");

            return Task.CompletedTask;
        }

        public Task<ServiceBindingResource> FetchAsync(string instanceId, string bindingId)
        {
            throw new System.NotImplementedException();
        }

        private static void LogContext(ILogger log, string operation, ServiceBindingContext context)
        {
            log.LogInformation(
                $"{operation} - context: {{ instance_id = {context.InstanceId}, " +
                                          $"binding_id = {context.BindingId}, " +
                                          $"originating_identity = {{ platform = {context.OriginatingIdentity?.Platform}, " +
                                                                    $"value = {context.OriginatingIdentity?.Value} }} }}");
        }

        private static void LogRequest(ILogger log, ServiceBindingRequest context)
        {
            log.LogInformation(
                $"Bind - request: {{ bind_resource = {{ app_guid = {context.BindResource.AppGuid}, " +
                                                      $"route = {context.BindResource.Route} }}, " +
                                   $"context = {context.Context}, " +
                                   $"parameters = {context.Parameters}, " +
                                   $"plan_id = {context.PlanId}, " +
                                   $"service_id = {context.ServiceId} }}");
        }
    }
}
