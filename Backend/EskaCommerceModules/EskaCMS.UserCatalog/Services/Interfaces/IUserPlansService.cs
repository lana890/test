using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.UserCatalog.Dtos;
using EskaCMS.Entities.Entity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.UserCatalog.Services.Interfaces
{
    public interface IUserPlansService
    {
        UserPlansOutput Get(long Id);
        Task<PlansVersionsOutput> GetPlanLatestVersion(long planId);
        Task Update(UserPlansUpdate userPlan);
        Task<IEnumerable<UserPlansOutput>> GetUserPlans(long SiteId);
        Task<IList<UserPlansOutput>> GetUserPlansById(long SiteId, long UserId);
        Task<SmartTableResult<UserPlansOutput>> GetAll(SmartTableParam input);
        Task<IEnumerable<UserInvoicesOutputVM>> GetUserInvoices(long siteId);
        Task<IList<UserPlansOutput>> GetUserPlanByOrderId(long OrderId);
        Task<PlansVersionsOutput> GetLatestVersion(long PlanVersionId);
        Task<List<UserPlansHistoryVM>> GetUserPlanHistoryByUserPlanId(long UserPlanId);
        Task<UserPlansOutput> GetUserPlanByUserPlanId(long UserPlanId);
        Task AddUserPlanProducts(long UserPlanId, long OrderId);
        Task<IEnumerable<UserPlansOutput>> GetUserProfileServices(long siteId);
        Task<long> Create(long planId, long siteId, long OrderId, string AdditionInfo, EUserPlansStatus CurrentStatus, EUserPlansStatus NextStatus, bool? IsOneTimeService = null, long? parentId = null);
    }

}
