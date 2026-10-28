using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.UserCatalog.Dtos;
using System.Threading.Tasks;

namespace EskaCommerce.UserCatalog.Services.Interfaces
{
    public interface IPlansVersionsService
    {
        Task<long> Create(PlansVersionsCreate plansVersionsInput, long siteId);
        PlansVersionsOutput Get(long id);
        Task<PlansVersionsVM> PlanVersionDetails(long id);
        Task<SmartTableResult<PlansVersionsOutput>> GetPlanVersions(long planId);
        long GetLatestVersionId(long planId);
        Task<bool> Delete(long id);
    }

}
