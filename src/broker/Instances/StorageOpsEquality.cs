using Newtonsoft.Json.Linq;
using operations;

namespace broker.Instances
{
    public class StorageOpsEquality : OpsEquality
    {
        protected override bool OpsEquals((JObject xContext, JObject xParameters) x, (JObject xContext, JObject xParameters) y) => true;

        protected override int OpsHashCode((JObject context, JObject parameters) obj) => 0;
    }
}
