using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Services.Interfaces
{
    public  interface IAlertServices
    {
        Task SendRealTimeAlert(long[] UserIds,string CustomerGroupName,long CustomerGroupId,string FullName,string UserName,long GroupTypeId);

    }
}
