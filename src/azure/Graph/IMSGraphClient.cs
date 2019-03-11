using System.Threading;
using System.Threading.Tasks;
using azure.Graph.Model;

namespace azure.Graph
{
    public interface IMSGraphClient
    {
        Task<Application> CreateApplication(Application application, CancellationToken ct = default);

        Task<ServicePrincipal> CreateServicePrincipal(ServicePrincipal servicePrincipal, CancellationToken ct = default);

        Task DeleteApplication(string name, CancellationToken ct = default);
    }
}
