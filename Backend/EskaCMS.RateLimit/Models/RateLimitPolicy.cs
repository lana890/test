using System.Collections.Generic;

namespace EskaCMS.RateLimit.Models
{
    public class RateLimitPolicy
    {
        public List<RateLimitRule> Rules { get; set; } = new List<RateLimitRule>();
    }
}
