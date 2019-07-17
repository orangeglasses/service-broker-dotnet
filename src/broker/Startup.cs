using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using azure;
using azure.Config;
using broker.azure.storage.Instances;
using broker.Bindings;
using broker.Instances;
using idunno.Authentication.Basic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using operations;
using OpenServiceBroker;
using OpenServiceBroker.Bindings;
using OpenServiceBroker.Catalogs;
using OpenServiceBroker.Instances;
using Steeltoe.Extensions.Configuration.CloudFoundry;

namespace broker
{
    public class Startup
    {
        private readonly IHostingEnvironment _env;

        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            _env = env;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();

            services
                .AddMvc(options =>
                {
                    // Add authorize filter globally for all controllers.
                    options.Filters.Add(new AuthorizeFilter());
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddSingleton<IInstanceOps, Ops>();

            services
                .AddTransient<ICatalogService, CatalogService>()
                .AddTransient<IServiceInstanceBlocking, ServiceInstanceBlocking>()
                .AddTransient<IServiceInstanceDeferred, ServiceInstanceDeferred>()
                .AddTransient<IServiceBindingBlocking, ServiceBindingBlocking>()
                .AddTransient<ProvisioningOpEquality, StorageProvisioningOpEquality>()
                .AddOpenServiceBroker();

            services
                .AddAuthentication(BasicAuthenticationDefaults.AuthenticationScheme)
                .AddBasic(options =>
                {
                    // We need to allow insecure basic http authentication, which is generally a bad idea.
                    // However, PCF terminates SSL at the load balancer and sends requests to the application
                    // via plain http so we have no choice.
                    options.AllowInsecureProtocol = true;

                    options.Realm = "broker";
                    options.Events = new BasicAuthenticationEvents
                    {
                        OnValidateCredentials = context =>
                        {
                            var password = Configuration["Authentication:Password"];
                            if (context.Username == "rwwilden-broker" && context.Password == password)
                            {
                                // Generate principal.
                                var claims = new[]
                                {
                                    new Claim(ClaimTypes.NameIdentifier,
                                        context.Username, ClaimValueTypes.String, context.Options.ClaimsIssuer),
                                    new Claim(ClaimTypes.Name,
                                        context.Username, ClaimValueTypes.String, context.Options.ClaimsIssuer)
                                };
                                context.Principal = new ClaimsPrincipal(
                                    new ClaimsIdentity(
                                        claims,
                                        context.Scheme.Name
                                    )
                                );

                                context.Success();
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            // Add Cloud Foundry options.
            services.ConfigureCloudFoundryOptions(Configuration);

            // Add Azure REST API services.
            services.AddAzureServices(
                ConfigureAzure(services, _env),
                ConfigureAzureAuth(services, _env));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseAuthentication();

            app.UseHttpsRedirection();
            app.UseMvc();
        }

        private Action<AzureOptions> ConfigureAzure(IServiceCollection services, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                return options => Configuration.Bind("Azure", options);
            }

            return options =>
            {
                // Get CF services (VCAP_SERVICES) and azure service.
                var serviceProvider = services.BuildServiceProvider();
                var cfServicesOptions = serviceProvider.GetRequiredService<IOptions<CloudFoundryServicesOptions>>();
                var cfServices = cfServicesOptions.Value;
                var userScopedServices = cfServices.Services["user-provided"];
                var azureService = userScopedServices.Single(service => service.Name == "azure");

                // Get credentials for azure service.
                var credentials = azureService.Credentials;

                // Configure options.
                options.SubscriptionId = credentials["SubscriptionId"].Value;
            };
        }

        private Action<ConfidentialClientApplicationOptions> ConfigureAzureAuth(IServiceCollection services, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                return options => Configuration.Bind("AzureAuth", options);
            }

            return options =>
            {
                // Get CF services (VCAP_SERVICES) and azure-auth service.
                var serviceProvider = services.BuildServiceProvider();
                var cfServicesOptions = serviceProvider.GetRequiredService<IOptions<CloudFoundryServicesOptions>>();
                var cfServices = cfServicesOptions.Value;
                var userScopedServices = cfServices.Services["user-provided"];
                var azureAuthService = userScopedServices.Single(service => service.Name == "azure-auth");

                // Get credentials for azure-auth service.
                var credentials = azureAuthService.Credentials;

                // Configure options.
                options.ClientId = credentials["ClientId"].Value;
                options.ClientSecret = credentials["ClientSecret"].Value;
                options.TenantId = credentials["TenantId"].Value;
            };
        }
    }
}
