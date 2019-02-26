using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Extensions.Configuration.CloudFoundry;

namespace broker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args)
                .Build()
                .Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    var cloudFoundryEnvironmentSettingsReader =
                        context.HostingEnvironment.IsDevelopment()
                            ? (ICloudFoundrySettingsReader) new CloudFoundryMemorySettingsReader
                            {
                                ServicesJson = @"{
'user-provided': [
{ 'binding_name': null,
  'credentials':
  { 'ClientId': 'b2213c77-9d93-474b-9b7f-89a1f0040162',
    'ClientSecret': '<secret>',
    'Instance': 'https://login.microsoftonline.com/',
    'TenantId': 'e402c5fb-58e9-48c3-b567-741c4cef0b96' },
  'instance_name': 'azure-rm-auth',
  'label': 'user-provided',
  'name': 'azure-rm-auth',
  'syslog_drain_url': '',
  'tags': [],
  'volume_mounts': [] },
{ 'binding_name': null,
  'credentials':
  { 'SubscriptionId': '4c70a177-b978-43f9-9fc0-1e50dd20271f' },
  'instance_name': 'azure',
  'label': 'user-provided',
  'name': 'azure',
  'syslog_drain_url': '',
  'tags': [],
  'volume_mounts': [] } ] }"
                            }
                            : new CloudFoundryEnvironmentSettingsReader();

                    // Exposes VCAP_* environment variables to application.
                    builder.AddCloudFoundry(cloudFoundryEnvironmentSettingsReader);
                })
                .UseCloudFoundryHosting()
                .ConfigureLogging(builder =>
                {
                    builder
                        .ClearProviders()
                        .AddConsole();
                })
                .UseStartup<Startup>();
    }
}
