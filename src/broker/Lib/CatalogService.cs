using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OpenServiceBroker.Catalogs;

namespace broker.Lib
{
    public class CatalogService : ICatalogService
    {
        private const string ServiceId = "7e17396b-a696-4459-af1f-b35b875ee81b";
        private const string BasicPlanId = "fe342717-b157-48f1-b9b2-e3e7b8057170";
        private const string PremiumPlanId = "cf9f1e19-2750-4978-a8c9-8cea4b9a11ab";

        private static readonly JObject ServiceMetadata =
            JObject.FromObject(
                new
                {
                    displayName = "rwwilden service",
                    longDescription = "The magnificent and well-known rwwilden service"
                });

        private static readonly JObject BasicPlanMetadata =
            JObject.FromObject(
                new {
                    displayName = "Basic plan"
                });

        private static readonly Task<Catalog> CatalogTask = Task.FromResult(
            new Catalog
            {
                Services =
                {
                    // https://github.com/openservicebrokerapi/servicebroker/blob/v2.14/spec.md#service-object
                    new Service
                    {
                        Id = ServiceId,
                        Name = "rwwilden",
                        Description = "The magnificent and well-known rwwilden service",

                        // This service broker now has support for service binding so we will set this property to true.
                        Bindable = true,
                        BindingsRetrievable = false,

                        // This service broker will be used to provision instances so fetching them should also be supported.
                        InstancesRetrievable = true,

                        // No support yet for service plan updates.
                        PlanUpdateable = false,

                        Metadata = ServiceMetadata,

                        Plans =
                        {
                            new Plan
                            {
                                Id = BasicPlanId,
                                Name = "basic",
                                Description = "Basic plan",
                                Bindable = true,
                                Free = true,
                                Metadata = BasicPlanMetadata
                            }
                        }
                    }
                }
            });

        public Task<Catalog> GetCatalogAsync() => CatalogTask;
    }
}
