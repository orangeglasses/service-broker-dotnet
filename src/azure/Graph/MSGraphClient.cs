using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using azure.Errors;
using azure.Graph.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace azure.Graph
{
    internal class MSGraphClient : AzureClient, IMSGraphClient
    {
        public MSGraphClient(HttpClient client, ILogger<MSGraphClient> log)
            : base(client, log)
        {
        }

        public async Task<Application> CreateApplication(Application application, CancellationToken ct = default)
        {
            var serializedApplication = JsonConvert.SerializeObject(application, Formatting.None, JsonSerializerSettings);

            var request = new HttpRequestMessage(HttpMethod.Post, "applications")
            {
                Content = new StringContent(serializedApplication, Encoding.UTF8, "application/json")
            };

            using (request)
            using (var response = await Client.SendAsync(request, ct))
            {
                using (var responseStream = await response.Content.ReadAsStreamAsync())
                using (var streamReader = new StreamReader(responseStream))
                using (var jsonTextReader = new JsonTextReader(streamReader))
                {
                    if (response.StatusCode == HttpStatusCode.Created)
                    {
                        return JsonSerializer.Deserialize<Application>(jsonTextReader);
                    }

                    var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(jsonTextReader);
                    throw new AzureResourceException("Unexpected status code", response.StatusCode, errorResponse.Error);
                }
            }
        }

        public async Task<ServicePrincipal> CreateServicePrincipal(ServicePrincipal servicePrincipal, CancellationToken ct = default)
        {
            var serializedServicePrincipal =
                JsonConvert.SerializeObject(servicePrincipal, Formatting.None, JsonSerializerSettings);

            var request = new HttpRequestMessage(HttpMethod.Post, "serviceprincipals")
            {
                Content = new StringContent(serializedServicePrincipal, Encoding.UTF8, "application/json")
            };

            using (request)
            using (var response = await Client.SendAsync(request, ct))
            {
                using (var responseStream = await response.Content.ReadAsStreamAsync())
                using (var streamReader = new StreamReader(responseStream))
                using (var jsonTextReader = new JsonTextReader(streamReader))
                {
                    if (response.StatusCode == HttpStatusCode.Created)
                    {
                        return JsonSerializer.Deserialize<ServicePrincipal>(jsonTextReader);
                    }

                    var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(jsonTextReader);
                    throw new AzureResourceException("Unexpected status code", response.StatusCode, errorResponse.Error);
                }
            }
        }
    }
}
