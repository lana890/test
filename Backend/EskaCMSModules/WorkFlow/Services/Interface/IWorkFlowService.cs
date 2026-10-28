using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.WorkFlow.Models.Request;
using EskaCMS.WorkFlow.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.WorkFlow.Services.Interface
{
    public interface IWorkFlowService
    {
        Task<bool> CreateWorkFlow(CreateWorkFlowDto model);
        Task<bool> EditWorkFlow(EditWorkFlowDto model);
        Task<bool> ActiveWorkFlow(long Id);
        Task<WorkFlowDetailsDto> GetWorkFlowDetails(long Id);
        Task<bool> DeActivateWorkFlow(long Id);
        Task<bool> DeleteWorkFlow(long Id);
        Task<PaginationResult<WorkFlowDetailsDto>> GetAllWorkFlows(SmartTableParam param);
    }
}
