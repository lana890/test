using EskaCMS.Core.BusinessModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Models;
using EskaCMS.CoreShared.ViewModels;

using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using static BusinessModels.BusinessModel;
using EskaCMS.Infrastructure.Pagination;
using EskaCms.CoreShared.ViewModels;

namespace EskaCMS.CoreShared.Services
{
    public interface IUserApiService
    {

        Task<long> Create(UserForm model, long siteId);
        Task<long> Update(UserForm model, long siteId, long userId, IFormFileCollection files);
        Task EditUserOption(UserOptionsVM userOptionsVM);
        Task DeleteUserOption(long id);
        Task Delete(long id);
        Task EditUserCurrencyAndCulture(EditUserCurrencyVM Model);
        Task<UserDetailsVM> GetById(long id);
        Task<UserGridOutputVM<UserViewModel>> GetList(UserSearchTableParam<UserListSearchVM> param, Entities.Enum.GeneralEnums.FillterType? fillterType);
        Task<List<UserGetAllVM>> GetAll();
        Task<PaginationResult<QuickSearchVM>> QuickSearch(UserSearchOption searchOption,long siteId);
        Task<bool> SyncADUsers();
        Task<bool> UploadUsers(IFormFile formFile, UploadUsers uploadUsers);

        Task<List<UserDDLVM>> GetUsersDDL(UserSearchTableParam<UserGetddlVM> param);

        Task<UserBasicInfoVM> GetBasicinfo(long userId);
        Task<List<ExportUsersData>> ExportUsers(UserSearchTableParam<UserListSearchVM> param, Entities.Enum.GeneralEnums.FillterType? fillterType);
        Task<bool> UpdateBasicinfo(UserBasicInfoVM model, long userId);
        Task<string> GetUserCode(long id);
        Task<bool> EditUserCountry(EditUserCountry Model);
        Task<UserCountryVM> GetUserCountry();
        Task<bool> CheckReferrerCode(ReferrerCodeVM code);
        Task<PaginationResult<UserListAdvanceVM>> GetListWithAdvancedSearch(UserSearchTableParam<UserListAdvanceSearchVM> param);
        Task<List<GetUsersWithOptionsVM>> GetAllUsersOptions(GetUserOptionByPropertyVM model);
        Task<PaginationResult<DepartmentEmployeeViewModel>> GetAllEmployees(FilterUserVM model);
        Task<List<UserDetailsVM>> GetUserChildren(long id);
    }
}
