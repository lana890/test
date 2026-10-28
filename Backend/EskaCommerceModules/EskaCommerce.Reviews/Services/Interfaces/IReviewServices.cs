using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Reviews.Areas.Reviews.ViewModels;
using EskaCommerce.Reviews.Models;
using EskaCommerce.Reviews.Models.ReviewModels;
using EskaCommerce.Reviews.Models.ReviewModels.ReviewPosts;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Reviews.Services.Interfaces
{
    public interface IReviewServices
    {
        #region Web Products


        Task<bool> AddReview(ReviewForm model, List<IFormFile> files);
        Task<ReviewVm> List(long ProductId,int PageNumber, int PageSize);
        #endregion

        #region web Posts

        Task<ReviewPostsVm> ListPosts(long PostId, int PageSize, int PageNumber);
        Task<bool> AddReviewPosts(ReviewPostVM model);
        Task<bool> RemoveReview(long Id);
        #endregion

        #region Admin 
        Task<bool> ChangeStatus(long id, long status);
        Task<List<ReviewDetailsViewModel>> GetProductReviewer(long productId);
        Task<PaginationResult<ReviewViewModel>> Grid(SmartTableParam param);
        Task<bool> BlockUserPermissions(long userId);
        Task<bool> UnBlockUserPermissions(long userId);
        Task<PaginationResult<UsersBlockedViewModel>> GetUserBlocked(UserBlockedViewModelRequest  model);
        #endregion
    }
}
