using Microsoft.Extensions.Logging;
using EskaCMS.Core.Extensions;
using EskaCMS.SignalR.RealTime;

namespace EskaCMS.SignalR.Hubs
{
    public class CommonHub : OnlineClientHubBase
    {
        public CommonHub(IWorkContext workContext, IOnlineClientManager onlineClientManager) : base(workContext, onlineClientManager)
        {

        }

        public void Register()
        {
            Logger.LogDebug("A client is registered: " + Context.ConnectionId);
        }
        public string GetConnectionId() => Context.ConnectionId;
    }
}
