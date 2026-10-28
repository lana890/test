using System.Collections.Generic;

namespace EskaCMS.RateLimit.Models
{
    public class ClientRateLimitPolicies
    {
        public List<ClientRateLimitPolicy> ClientRules { get; set; }
    }
}