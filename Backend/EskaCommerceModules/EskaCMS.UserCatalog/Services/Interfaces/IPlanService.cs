using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.UserCatalog.Dtos;
using EskaCommerce.UserCatalog.Dtos.CustomePlan;
using System.Collections.Generic;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.UserCatalog.Services.Interfaces
{
    public interface IPlanService
    {
        Task<long> Create(PlanCreate planInput, long siteId);
        Task<PlanOutput> Get(long id, long siteId);
        Task<PlanOutput> GetPlanWithProducts(long id, long siteId);
        Task Update(PlanUpdate plan);

        Task<IList<PlanOutput>> GetPlansList(long siteId);
        //    Task<SmartTableResult<PlanOutput>> GetAll(SmartTableParam input);
        Task<IList<PlanOutput>> GetPlansByIds(IList<long> ids, long siteId);
        Task<PlanOutput> GetPlanById(long id);
        Task<IList<PlanOutput>> GetOneTimePlans(long siteId);
        Task<IList<PlanOutput>> GetRecurringPlans(long siteId);
        Task<PlanDetailsVM> GetPlanDetails(long id, long siteId);
        Task<PaginationResult<PlanResponseDto>> GetAll(SmartTableParam param, bool GetCustomePlan);
        Task<long> CreateCustomePlan(CustomePlanDto model);
    }

}
