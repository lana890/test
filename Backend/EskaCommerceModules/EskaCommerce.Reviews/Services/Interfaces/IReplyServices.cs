using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Reviews.Areas.Reviews.ViewModels;
using EskaCommerce.Reviews.Models.ReplayModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Reviews.Services.Interfaces
{
    public interface IReplyServices
    {
        #region Reply Admin
        Task<bool> ChangeStatus(long id, long status);
        Task<PaginationResult<ReplyViewModel>> Grid(SmartTableParam param);
        #endregion
        #region Web
        Task<bool> Insert(ReplyForm model);

        Task<bool> Delete(long replyId);
        #endregion
    }
}
