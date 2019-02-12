using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using broker.Lib;
using idunno.Authentication.Basic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using OpenServiceBroker;
using OpenServiceBroker.Bindings;
using OpenServiceBroker.Catalogs;
using OpenServiceBroker.Instances;

namespace broker
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc(options =>
                {
                    // Add authorize filter globally for all controllers.
                    options.Filters.Add(new AuthorizeFilter());
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services
                .AddTransient<ICatalogService, CatalogService>()
                .AddTransient<IServiceInstanceBlocking, ServiceInstanceBlocking>()
                .AddTransient<IServiceBindingBlocking, ServiceBindingBlocking>()
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

            services
                .Configure<AzureConfig>(Configuration.GetSection("Azure"))
                .Configure<AzureAdConfig>(Configuration.GetSection("AzureAd"));

            services.AddTransient<AuthorizationHandler>();
            services
                .AddHttpClient<ResourceGroupClient>(async client =>
                {

                    var authenticationResult =
                        await clientApp.AcquireTokenForClientAsync(new[] { "https://management.core.windows.net/.default" });

                    client.BaseAddress =
                        new Uri($"https://management.azure.com/subscriptions/{azureConfig.SubscriptionId}/resourcegroups/");
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", authenticationResult.AccessToken);
                })
                .AddHttpMessageHandler<AuthorizationHandler>();
        }

        public class AuthorizationHandler : DelegatingHandler
        {
            private IConfidentialClientApplication _clientApplication;

            public AuthorizationHandler(IOptions<AzureAdConfig> azureAdConfigOptions)
            {
                var azureAdConfig = azureAdConfigOptions.Value;

                _clientApplication = new ConfidentialClientApplication(
                    azureAdConfig.ClientId,
                    $"{azureAdConfig.Instance}{azureAdConfig.TenantId}",
                    azureAdConfig.RedirectUri,
                    new ClientCredential(azureAdConfig.ClientSecret),
                    null,
                    new TokenCache());
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                // Get authorization token from Azure AD.
                var authenticationResult =
                    await _clientApplication.AcquireTokenForClientAsync(new[] { "https://management.core.windows.net/.default" } );

                return await base.SendAsync(request, ct);
            }
        }

        public class AzureConfig
        {
            public string SubscriptionId { get; set; }
        }

        public class AzureAdConfig
        {
            public string Instance { get; set; }
            public string TenantId { get; set; }
            public string ClientId { get; set; }
            public string ClientSecret { get; set; }
            public string RedirectUri { get; set; }
        }

        public class ResourceGroupClient
        {
            public ResourceGroupClient(HttpClient client)
            {
            }
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
    }
}
