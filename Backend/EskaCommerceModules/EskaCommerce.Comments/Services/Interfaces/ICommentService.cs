using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Comments.Models.Request;
using EskaCommerce.Comments.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Comments.Services.Interfaces
{
    public interface ICommentService
    {
        #region Admin
        Task<bool> ChangeStatus(long id, int statusId);
        Task<PaginationResult<CommentAdminViewModel>> Grid(SmartTableParam model);
        #endregion

        #region Web Products
        Task<bool> Create(CommentRequestViewModel model);
        Task<PaginationResult<CommentWebViewModel>> GetComments(GetRequestViewModel model);
        #endregion

        #region Web Posts
        Task<PaginationResult<CommentWebViewModel>> GetCommentPosts(CommentRequestVM model);
        Task<bool> CreatePosts(CommentPostsRequestViewModel model);
        #endregion
    }
}
