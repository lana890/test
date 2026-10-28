using AutoMapper;
using AutoMapper.QueryableExtensions;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Comments;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Reviews;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Comments.Models.Request;
using EskaCommerce.Comments.Models.Response;
using EskaCommerce.Comments.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Comments.Services
{
    public class CommentServices : ICommentService
    {
        private readonly IRepository<Comment> commentRepository;
        private readonly IRepository<CommentProduct> commentProductRepository;
        private readonly IRepository<CommentPost> commentPostRepository;
        private readonly IRepository<UserInteractionConfiguration> userInteractionRepopsitory;
        private readonly IRepository<UsersSites> userSite;
        private readonly IRepository<User> userRepository;
        private readonly IRepository<Order> orderRepository;
        private readonly IWorkContext workContext;
        private readonly IMapper mapper;

        public CommentServices(IRepository<Comment> CommentRepository
            ,IRepository<CommentProduct> CommentProductRepository
            ,IRepository<CommentPost> CommentPostRepository
            , IRepository<UserInteractionConfiguration> UserInteractionRepopsitory
            ,IRepository<UsersSites> UserSite
            , IRepository<User> UserRepository
            , IRepository<Order> OrderRepository,
            IWorkContext workContext,IMapper mapper)
        {
            commentRepository = CommentRepository;
            commentProductRepository = CommentProductRepository;
            commentPostRepository = CommentPostRepository;
            userInteractionRepopsitory = UserInteractionRepopsitory;
            userSite = UserSite;
            userRepository = UserRepository;
            orderRepository = OrderRepository;
            this.workContext = workContext;
            this.mapper = mapper;
        }

        #region Admin
        public async Task<bool> ChangeStatus(long id, int statusId)
        {
            try
            {
                var comment = await commentRepository.Query().Where(x => x.Id == id).FirstOrDefaultAsync();

                if (comment == null)
                {
                    throw new Exception("Not Found");
                }

                comment.Status = (CommentStatus)statusId;

                await commentRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<PaginationResult<CommentAdminViewModel>> Grid(SmartTableParam model)
        {
            try
            {
                var query = await commentRepository
                                     .Query()
                                     .Include(x => x.User)
                                     .Include(x=>x.CommentProducts)
                                     .Include(x=>x.Replies)
                                     .FilterComment(model)
                                     .AsNoTrackingWithIdentityResolution()
                                     .ProjectTo<CommentAdminViewModel>(mapper.ConfigurationProvider)
                                     .OrderBy(x => x.Id)
                                     .PaginateAsync(model.Pagination.Number, model.Pagination.Start);
                return query;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #endregion

        #region Web Products

        public async Task<bool> Create(CommentRequestViewModel model)
        {
            try
            {
                var UserId = await workContext.GetCurrentUserId();
                var SiteId =  workContext.GetCurrentSiteId();

                var UserSite = await userSite.Query().Where(x => x.UserId == UserId && x.SiteId == SiteId).FirstOrDefaultAsync();

                if (!UserSite.CommentPermissions)
                {

                    throw new Exception("We regret to inform you that your commenting privileges have been temporarily suspended");

                }

                await CheckMaxComment(model.ProductId, UserId);

                var comment = new Comment
                {
                    ParentId = model.ParentId==0|| model.ParentId==null?null:model.ParentId,
                    CommentText = model.Comment,
                    Status = CommentStatus.Pending,
                    UserId = UserId,
                    CreatedOn = DateTime.Now
                };

                commentRepository.Add(comment);
                await commentRepository.SaveChangesAsync();

                await MappingCommentProduct(model.ProductId,comment.Id);

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        public async Task<PaginationResult<CommentWebViewModel>> GetComments(GetRequestViewModel model)
        {
            try
            {

                var query = commentProductRepository.Query()
                    .Include(x=>x.Comment).ThenInclude(x=>x.Replies)
                    .Where(x => x.Comment.ParentId == null&&(model.ProductId==null||x.ProductId==model.ProductId));
                var Result = await query.Where(x => ( x.Comment.Status == CommentStatus.Approved)
                                              && (string.IsNullOrEmpty(model.SearchText) || x.Comment.CommentText.Contains(model.SearchText)))
                                              .AsNoTrackingWithIdentityResolution()
                                              .Select(x=>x.Comment)
                                              .ProjectTo<CommentWebViewModel>(mapper.ConfigurationProvider)
                                              .PaginateAsync(model.pagination.PageSize, model.pagination.PageNumber);
              return Result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        #endregion


        #region Web Posts

        public async Task<bool> CreatePosts(CommentPostsRequestViewModel model)
        {
            try
            {
                var UserId = await workContext.GetCurrentUserId();
                var SiteId = workContext.GetCurrentSiteId();

                var UserSite = await userSite.Query().Where(x => x.UserId == UserId && x.SiteId == SiteId).FirstOrDefaultAsync();

                if (!UserSite.CommentPermissions)
                {

                    throw new Exception("We regret to inform you that your commenting privileges have been temporarily suspended");

                }

                await CheckMaxPostsComment(model.PostId, UserId);

                var comment = new Comment
                {
                    ParentId = model.ParentId == 0 || model.ParentId == null ? null : model.ParentId,
                    CommentText = model.Comment,
                    Status = CommentStatus.Pending,
                    UserId = UserId,
                    CreatedOn = DateTime.Now
                };

                commentRepository.Add(comment);
                await commentRepository.SaveChangesAsync();

                await MappingCommentPosts(model.PostId, comment.Id);

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

    

        public async Task<PaginationResult<CommentWebViewModel>> GetCommentPosts(CommentRequestVM model)
        {
            try
            {

                var query = commentPostRepository.Query()
                                                     .Include(x => x.Comment)
                                                     .ThenInclude(x => x.Replies)
                                                     .Where(x => x.Comment.ParentId == null && 
                                                     (model.PostId==null||x.PostId == model.PostId));
                var Result = await query.Where(x => (x.Comment.Status == CommentStatus.Approved)
                                              && (string.IsNullOrEmpty(model.SearchText) || x.Comment.CommentText.Contains(model.SearchText)))
                                              .AsNoTrackingWithIdentityResolution()
                                              .Select(x => x.Comment)
                                              .ProjectTo<CommentWebViewModel>(mapper.ConfigurationProvider)
                                              .PaginateAsync(model.pagination.PageSize, model.pagination.PageNumber);
                return Result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        #endregion



        #region Helpers Products
        private async Task MappingCommentProduct(long productId, long id)
        {
            try
            {
               
                  var Query =new CommentProduct
                    {
                        CommentId = id,
                        ProductId = productId

                  };

                
                commentProductRepository.Add(Query);
                await commentProductRepository.SaveChangesAsync();

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task CheckMaxComment(long ProductId, long UserId)
        {
            try
            {


                var RoleIds = await userRepository.Query()
                                           .Include(x => x.UserRoles)
                                           .Where(c => c.UserRoles.Any(x => x.UserId == UserId))
                                           .SelectMany(x => x.UserRoles)
                                           .Select(x => x.RoleId)
                                           .ToListAsync();

                var UserRole = RoleIds;



                var UserNotAdmin = UserRole.Any(x => x == 1);

                if (!UserNotAdmin)
                {
                    var Role = UserRole.FirstOrDefault();
                    var CommentConfiguration = await userInteractionRepopsitory.Query().Where(x => x.ERoleTypes == (ERoleTypes)Role).FirstOrDefaultAsync();
                    if (CommentConfiguration != null)
                    {

                        if (CommentConfiguration.AllowCommnetIfPayOrder)
                        {

                            await CheckIfUserPayThisProduct(ProductId, UserId);

                        }
                        await CheckMoreThanMaxProduct(ProductId, UserId, CommentConfiguration.MaxNumberOfComments);
                    }
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }


        }
        private async Task CheckIfUserPayThisProduct(long productId, long userId)
        {
            try
            {
                var Order = await orderRepository.Query().AsNoTracking().Include(x => x.OrderItems)
                                                       .Where(x => x.CustomerId == userId
                                                       && (x.OrderStatus != OrderStatus.PaymentFailed ||
                                                       x.OrderStatus != OrderStatus.Canceled ||
                                                       x.OrderStatus != OrderStatus.PendingPayment)).SelectMany(x => x.OrderItems).Where(x => x.ProductId == productId).FirstOrDefaultAsync();

                if (Order == null)
                {

                    throw new Exception("You Can Commented In This Product because You are Not Paid");

                }

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task CheckMoreThanMaxProduct(long ProductId, long UserId,long CommentReview)
        {
            try
            {
                var NumberOfReviewr = await commentProductRepository.Query().Include(x => x.Comment)
                                                                .Where(x => x.ProductId == ProductId)
                                                                .Select(x => x.Comment)
                                                                .Where(x => x.UserId == UserId)
                                                                .CountAsync();

                if (CommentReview < NumberOfReviewr + 1)
                {
                    throw new Exception("Comment limit reached");
                }



            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #endregion

        #region  Helpers Posts

        private async Task CheckMaxPostsComment(long PostId, long UserId)
        {
            try
            {


                var RoleIds = await userRepository.Query()
                                           .Include(x => x.UserRoles)
                                           .Where(c => c.UserRoles.Any(x => x.UserId == UserId))
                                           .SelectMany(x => x.UserRoles)
                                           .Select(x => x.RoleId)
                                           .ToListAsync();

                var UserRole = RoleIds;



                var UserNotAdmin = UserRole.Any(x => x == 1);

                if (!UserNotAdmin)
                {
                    var Role = UserRole.FirstOrDefault();
                    var CommentConfiguration = await userInteractionRepopsitory.Query().Where(x => x.ERoleTypes == (ERoleTypes)Role).FirstOrDefaultAsync();
                    if (CommentConfiguration != null)
                    { 
                        
                        await CheckMoreThanMaxPost(PostId, UserId, CommentConfiguration.MaxNumberOfComments);
                    }
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }


        }

        private async Task CheckMoreThanMaxPost(long PostId, long UserId, long CommentReview)
        {
            try
            {
                var NumberOfReviewr = await commentPostRepository.Query().Include(x => x.Comment)
                                                                .Where(x => x.PostId == PostId)
                                                                .Select(x => x.Comment)
                                                                .Where(x => x.UserId == UserId)
                                                                .CountAsync();

                if (CommentReview < NumberOfReviewr + 1)
                {
                    throw new Exception("Comment limit reached");
                }



            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        private async  Task MappingCommentPosts(long postId, long id)
        {
            try
            {

                var Query = new CommentPost
                {
                    CommentId = id,
                    PostId = postId

                };


                commentPostRepository.Add(Query);
                await commentPostRepository.SaveChangesAsync();

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #endregion
    }
}
