using EskaCMS.Core.Hubs;
using EskaCMS.Core.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Services
{
    public class AlertServices : IAlertServices
    {
        private readonly IHubContext<AlertHub> hubAlertHub;

        public AlertServices(IHubContext<AlertHub> hubAlertHub)
        {
            this.hubAlertHub = hubAlertHub;
        }


        public async Task SendRealTimeAlert(long[] UserIds, string CustomerGroupName, long CustomerGroupId, string FullName,string UserName,long GroupTypeId)
        {
            try
            {
                if (UserIds.Count() > 0)
                {
                    foreach (long userId in UserIds)
                    {
                        var isOnline = await AlertTracker.GetConnectionsForUser(userId);
                        if (isOnline != null || isOnline.Count() > 0)
                        {



                            var signalRClient = hubAlertHub.Clients.Clients(isOnline);
                            if (signalRClient == null)
                            {
                                continue;
                            }
                            await signalRClient.SendAsync("GetGroupAlertForUser", new {
                            
                                IsNewUser =true,
                                CustomerGroupName= CustomerGroupName,
                                CustomerGroupId= CustomerGroupId,
                                FullName= FullName,
                                UserName=UserName,
                                GroupTypeId= GroupTypeId

                            });

                        }

                    }

                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
}
