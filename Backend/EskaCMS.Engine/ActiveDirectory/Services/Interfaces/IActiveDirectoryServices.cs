using EskaCMS.Engine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Engine.Services.Interfaces
{
    public interface IActiveDirectoryServices
    {
        Task<UserViewModel> GetUserInfo(string UserName);
        Task<EskaCoreBaseResponseVM<CoreAuthResponseVM>> Authenticate();
        Task CreateUser(ActiveDirectoryViewModel adUser, HREmployee? employee);
        Task AddUserRolesSP(int userId, int userRoleId, long RoleType);
        Task UpdateParentSP(string UserName, int UserId);
        Task<long> CreateOrGetRoleAsync(string department, long RoleType, string connectionString);
        Task<long> CreateOrGetCustomerGroupAsync(string departmentEn, string departmentAr, string departmentId, string connectionString);
        Task AddUserGroupSP(int userId, int userGroupId);
        Task AddUpdateUserOptionsSP(long userId, string Name, string Value);
        Task<EskaCoreBaseResponseVM<List<CoreGroupViewModel>>> GetUserInfo(string authToken, string username);
        Task<EskaCoreBaseResponseVM> InsertUser(UserForm user, string token = "");
        Task UpdateUserGeneralInfo(long UserId, string Name, string PhoneNumber, string Email);
        Task DeleteUserSP(string samAccountName);
        Task<List<string>> GetAllUsernamesAsync();
    }
}
