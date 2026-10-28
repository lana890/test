namespace EskaCMS.RateLimit.Models
{
    public class IpRateLimitPolicy : RateLimitPolicy
    {
        public string Ip { get; set; }
    }
}