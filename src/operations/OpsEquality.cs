using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using OpenServiceBroker.Instances;

namespace operations
{
    public abstract class OpsEquality : IEqualityComparer<ProvisioningOperation>
    {
        private static readonly ServiceInstanceProvisionRequestComparer RequestComparer = new ServiceInstanceProvisionRequestComparer();

        public bool Equals(ProvisioningOperation x, ProvisioningOperation y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            var xReq = x.Request;
            var yReq = y.Request;
            
            return RequestComparer.Equals(xReq, yReq) &&
                   OpsEquals((xReq.Context, xReq.Parameters), (yReq.Context, yReq.Parameters));
        }

        public int GetHashCode(ProvisioningOperation obj)
        {
            unchecked
            {
                var hashCode = RequestComparer.GetHashCode(obj.Request);
                hashCode = (hashCode * 397) ^ OpsHashCode((obj.Request.Context, obj.Request.Parameters));
                return hashCode;
            }
        }

        protected abstract bool OpsEquals((JObject xContext, JObject xParameters) x, (JObject xContext, JObject xParameters) y);

        protected abstract int OpsHashCode((JObject context, JObject parameters) obj);

        private class ServiceInstanceProvisionRequestComparer : IEqualityComparer<ServiceInstanceProvisionRequest>
        {
            public bool Equals(ServiceInstanceProvisionRequest x, ServiceInstanceProvisionRequest y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x is null || y is null) return false;
                if (x.GetType() != y.GetType()) return false;

                return Equals(x.OrganizationGuid, y.OrganizationGuid) &&
                       Equals(x.SpaceGuid, y.SpaceGuid) &&
                       Equals(x.ServiceId, y.ServiceId) &&
                       Equals(x.PlanId, y.PlanId);
            }

            public int GetHashCode(ServiceInstanceProvisionRequest obj)
            {
                unchecked
                {
                    var hashCode = obj.OrganizationGuid == null ? 0 : obj.OrganizationGuid.GetHashCode();
                    hashCode = (hashCode * 397) ^ (obj.SpaceGuid == null ? 0 : obj.SpaceGuid.GetHashCode());
                    hashCode = (hashCode * 397) ^ (obj.ServiceId == null ? 0 : obj.ServiceId.GetHashCode());
                    hashCode = (hashCode * 397) ^ (obj.PlanId == null ? 0 : obj.PlanId.GetHashCode());
                    return hashCode;
                }
            }
        }
    }
}
