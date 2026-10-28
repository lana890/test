using EskaCMS.Entities.Entity.UserDashboard;
using EskaCMS.UserDashboard.Helpers;
using EskaCMS.UserDashboard.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.UserDashboard.Services.Interfaces
{
    public interface IUserDashboardService
    {
        Task<List<NavigationsByUserAndVisitResultVM>> GetNavigationsByUserAndVisit(NavigationsByUserAndVisitVM Model);

        Task<bool> ExecuteTakeNavigationSnapShot(UserNavigationVm userNavigationVm, SlugsTypes SlugType,long? ProductId);
        Task<GetDashBoardDataVM> GetNewAndReturningUsers(GetDashBoardDataVM Data);

        Task<Dictionary<string, UsersFlowVm>> GetUsersFlow(PeriodVM Period);

        Task<GetUserNavigationPaginationVM> GetUsersByNavigation(UsersByVisitVM Model);
        Task<List<UserVisitVM>> GetUserVisits(long UserId);
        Task<List<UserNavigations>> GetUserFlowByVisitId(long VisitId);
        Task<List<UserNavigationResultVM>> GetUserNavigations(UserNavigationVM userNavigationVM);
        Task<List<string>> GetBrowsers();
        Task<List<long>>  CheckLastFlowUser(long UserId,string UserIp);
    }
}

