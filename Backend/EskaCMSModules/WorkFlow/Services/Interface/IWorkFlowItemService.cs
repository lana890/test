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
    public interface IWorkFlowItemService
    {
        Task<PaginationResult<WorkFlowItemList>> GetAllWorkItem(SmartTableParam param, long workFlowId);
        Task<PaginationResult<WorkFlowItemList>> GetAllWorkItemPerUser(SmartTableParam param, long workFlowId);
        Task<WorkFlowItemDetailsDto> WorkFlowItemDetails(long workflowItemId);
        Task<bool> ActionOnWorkFlowItem(ActionOnItemDto model);
        Task<PaginationResult<WorkFlowItemDetailsDto>> GetAllWorkItemForWeb(SmartTableParam param);
        Task<long> CreateWorkFlowItem(CreateWorkFlowItemDto model);
        Task<bool> UpdateItemStatus(long itemId, long userId);
        Task<WorkFlowItemDetailsDto> GetLastWorkFlowItem();
    }
}
