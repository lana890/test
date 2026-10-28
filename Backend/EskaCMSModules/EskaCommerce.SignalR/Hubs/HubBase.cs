using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using EskaCMS.Core.Extensions;

namespace EskaCMS.SignalR.Hubs
{
    public abstract class HubBase : Hub
    {
        public ILogger<HubBase> Logger { get; set; }

        protected IWorkContext WorkContent { get; }

        protected HubBase(IWorkContext workContent)
        {
            Logger = NullLogger<HubBase>.Instance;
            WorkContent = workContent;
        }
    }
}
