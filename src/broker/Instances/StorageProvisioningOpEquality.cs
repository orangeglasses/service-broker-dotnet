using Newtonsoft.Json.Linq;
using operations;

namespace broker.Instances
{
    public class StorageProvisioningOpEquality : ProvisioningOpEquality
    {
        protected override bool OpsEquals((JObject xContext, JObject xParameters) x, (JObject yContext, JObject yParameters) y) => true;

        protected override int OpsHashCode((JObject context, JObject parameters) obj) => 0;
    }
}
