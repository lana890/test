using EskaCMS.Core.Areas.Core.ViewModels.GroupTypeModels.Request;
using EskaCMS.Core.Areas.Core.ViewModels.GroupTypeModels.Response;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Areas.Core.Interfaces
{
    public interface IGroupTypeService
    {
        Task<bool> Create(CreateVM model);
        Task<bool> Update(UpdateVM model);
        Task<PaginationResult<GroupTypeGridVM>> GetAll(SmartTableParam param);
        Task<GroupTypeGridVM> Details(long groupTypeId);
        Task<bool> Publish(long GroupTypeId);
        Task<bool> UnPublish(long GroupTypeId);
        Task<bool> Delete(long GroupTypeId);
    }
}
