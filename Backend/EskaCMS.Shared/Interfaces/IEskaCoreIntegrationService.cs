using EskaCMS.ADIntegration.ViewModels;
using EskaCMS.Core.BusinessModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EskaCMS.ADIntegration.Interfaces
{
    public interface IEskaCoreIntegrationService
    {
        Task<EskaCoreBaseResponseViewModel<CoreAuthResponseVM>> Authenticate();
        Task<EskaCoreBaseResponseViewModel> InsertUser(UserForm user,string token="");
        Task<EskaCoreBaseResponseViewModel> InsertUserGroup(InsertCoreGroupVM requestVM);
        Task<EskaCoreBaseResponseViewModel<List<CoreGroupViewModel>>> GetUserInfo(string authToken, string username);
        Task<EskaCoreBaseResponseViewModel> InsertUserBranch(InsertUserBranchVM requestVM);
    }
}
