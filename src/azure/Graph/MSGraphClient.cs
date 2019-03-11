using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using azure.Errors;
using azure.Graph.Model;
using azure.Lib;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace azure.Graph
{
    internal class MSGraphClient : AzureClient, IMSGraphClient
    {
        public MSGraphClient(HttpClient client, IHttp http, IJson json, ILogger<MSGraphClient> log)
            : base(client, http, json, log)
        {
        }

        public async Task<Application> CreateApplication(Application application, CancellationToken ct = default)
        {
            var serializedApplication =
                JsonConvert.SerializeObject(application, Formatting.None, Json.JsonSerializerSettings);
            var request = new HttpRequestMessage(HttpMethod.Post, "applications")
            {
                Content = new StringContent(serializedApplication, Encoding.UTF8, "application/json")
            };

            using (request)
            {
                var createdApplication = await Http.SendRequestAndDecodeResponse(
                    Client,
                    request,
                    (statusCode, jsonTextReader) =>
                    {
                        if (statusCode == HttpStatusCode.Created)
                        {
                            return Json.Deserialize<Application>(jsonTextReader);
                        }
                        var errorResponse = Json.Deserialize<ErrorResponse>(jsonTextReader);
                        throw new AzureResourceException(
                            "Unexpected status code for application create", statusCode, errorResponse.Error);
                    },
                    ct);
                return createdApplication;
            }
        }

        public async Task<ServicePrincipal> CreateServicePrincipal(ServicePrincipal servicePrincipal, CancellationToken ct = default)
        {
            var serializedServicePrincipal =
                JsonConvert.SerializeObject(servicePrincipal, Formatting.None, Json.JsonSerializerSettings);
            var request = new HttpRequestMessage(HttpMethod.Post, "serviceprincipals")
            {
                Content = new StringContent(serializedServicePrincipal, Encoding.UTF8, "application/json")
            };

            using (request)
            {
                var createdServicePrincipal =
                    await Http.SendRequestAndDecodeResponse(
                        Client,
                        request,
                        (statusCode, jsonTextReader) =>
                        {
                            if (statusCode == HttpStatusCode.Created)
                            {
                                return Json.Deserialize<ServicePrincipal>(jsonTextReader);
                            }

                            var errorResponse = Json.Deserialize<ErrorResponse>(jsonTextReader);
                            throw new AzureResourceException(
                                "Unexpected status code for service principal create", statusCode, errorResponse.Error);
                        },
                        ct);
                return createdServicePrincipal;
            }
        }

        public async Task DeleteApplication(string name, CancellationToken ct = default)
        {
            Application application = null;

            var clientBaseAddress = Client.BaseAddress;
            var nextPageUri = new Uri(clientBaseAddress, "applications");
            while (nextPageUri != null)
            {
                using (var getRequest = new HttpRequestMessage(HttpMethod.Get, nextPageUri))
                {
                    var applicationsPage =
                        await Http.SendRequestAndDecodeResponse(
                            Client,
                            getRequest,
                            (statusCode, jsonTextReader) =>
                            {
                                if (statusCode == HttpStatusCode.OK)
                                {
                                    return Json.Deserialize<ApplicationsPage>(jsonTextReader);
                                }

                                var errorResponse = Json.Deserialize<ErrorResponse>(jsonTextReader);
                                throw new AzureResourceException(
                                    "Unexpected status code for application list get", statusCode, errorResponse.Error);
                            },
                            ct);

                    // Scan applications for the right one. We can break out of the loop early
                    // if we find it.
                    application = applicationsPage.Applications
                        .SingleOrDefault(app => app.DisplayName == name);
                    if (application != null)
                    {
                        break;
                    }

                    nextPageUri = applicationsPage.NextPageUrl == null
                        ? null
                        : new Uri(applicationsPage.NextPageUrl);
                }
            }

            if (application == null)
            {
                var message = $"Could not find Azure AD application with name {name}";
                Log.LogWarning(message);
                throw new ArgumentException(message, nameof(name));
            }

            // We have found the right AD application, now delete it.
            using (var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"applications/{application.Id}"))
            {
                await Http.SendRequestAndDecodeResponse(
                    Client,
                    deleteRequest,
                    (statusCode, jsonTextReader) =>
                    {
                        if (statusCode != HttpStatusCode.NoContent)
                        {
                            var errorResponse = Json.Deserialize<ErrorResponse>(jsonTextReader);
                            throw new AzureResourceException(
                                "Unexpected status code for application delete", statusCode, errorResponse.Error);
                        }
                    },
                    ct);
            }
        }

        private class ApplicationsPage
        {
            [JsonProperty("value")]
            public Application[] Applications { get; private set; }

            [JsonProperty("@nextLink")]
            public string NextPageUrl { get; private set; }
        }
    }
}
