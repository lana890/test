using EskaCMS.RateLimit.CounterKeyBuilders;
using EskaCMS.RateLimit.Resolvers;
using System;
using System.Collections.Generic;

namespace EskaCMS.RateLimit.Middleware
{
    public interface IRateLimitConfiguration
    {
        IList<IClientResolveContributor> ClientResolvers { get; }

        IList<IIpResolveContributor> IpResolvers { get; }

        ICounterKeyBuilder EndpointCounterKeyBuilder { get; }

        Func<double> RateIncrementer { get; }

        void RegisterResolvers();
    }
}