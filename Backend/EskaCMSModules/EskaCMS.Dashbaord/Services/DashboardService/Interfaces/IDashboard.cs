using EskaCMS.Dashboard.Models.DashboardViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Dashboard.Services.DashboardService.Interfaces
{
    public interface IDashboard
    {
        Task<DashboardViewModel>  GetDashboardData(long UserId, long SiteId);
        Task<List<DailyVistsViewModel>> GetSiteGuest(long SiteId,long rangeType);
    }
}
