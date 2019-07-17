using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using OpenServiceBroker.Instances;

namespace operations
{
    public class DeprovisioningOpEquality : IEqualityComparer<DeprovisioningOperation>
    {
        public bool Equals(DeprovisioningOperation x, DeprovisioningOperation y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            if (x.GetType() != y.GetType()) return false;

            return string.Equals(x.ServiceId, y.ServiceId) && string.Equals(x.PlanId, y.PlanId);
        }

        public int GetHashCode(DeprovisioningOperation obj)
        {
            unchecked
            {
                return ((obj.ServiceId != null ? obj.ServiceId.GetHashCode() : 0) * 397) ^ (obj.PlanId != null ? obj.PlanId.GetHashCode() : 0);
            }
        }
    }

    public abstract class ProvisioningOpEquality : IEqualityComparer<ProvisioningOperation>
    {
        private static readonly ServiceInstanceProvisionRequestComparer RequestComparer = new ServiceInstanceProvisionRequestComparer();

        public bool Equals(ProvisioningOperation x, ProvisioningOperation y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            if (x.GetType() != y.GetType()) return false;

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

        protected abstract bool OpsEquals((JObject xContext, JObject xParameters) x, (JObject yContext, JObject yParameters) y);

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
