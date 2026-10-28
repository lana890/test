using EskaCommerce.UserCatalog.Dtos;
using System.Threading.Tasks;
using EskaCMS.Infrastructure.Web.SmartTable;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace EskaCommerce.UserCatalog.Services.Interfaces
{
    public interface IPlanProductVersionService
    {
        Task<IActionResult> Create(PlanProductVersionCreate planProductVersion);
        PlanProductVersionOutput Get(long id);
        Task Update(PlanProductVersionUpdate planProductVersion);
        Task<SmartTableResult<PlanProductVersionOutput>> GetAll(SmartTableParam input);
        Task<List<PlanProductVersionOutput>> GetProductsByPlan(long PlanId);

    }

}
