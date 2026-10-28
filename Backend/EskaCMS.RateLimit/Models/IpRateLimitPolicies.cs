using System.Collections.Generic;

namespace EskaCMS.RateLimit.Models
{
    public class IpRateLimitPolicies
    {
        public List<IpRateLimitPolicy> IpRules { get; set; } = new List<IpRateLimitPolicy>();
    }
}