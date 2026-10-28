using AutoMapper;
using AutoMapper.QueryableExtensions;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Reviews;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Reviews.Areas.Reviews.ViewModels;
using EskaCommerce.Reviews.Extensions;
using EskaCommerce.Reviews.Models.ReplayModels;
using EskaCommerce.Reviews.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Media.Services.Interfaces;
using static EskaCMS.Entities.Enum.GeneralEnums;
using IMediaService = EskaCMS.Media.Services.Interfaces.IMediaService;
using EskaCMS.Media.Models;

namespace EskaCommerce.Reviews.Services
{
    public class ReplyServices : IReplyServices
    {
        private readonly IRepository<Reply> replyRepository;
        private readonly IRepository<Review> reviewRepository;
        private readonly IRepository<UserInteractionConfiguration> userInteractionRepopsitory;
        private readonly IRepository<Order> orderRepository;
        private readonly IRepository<User> userRepository;
        private readonly IRepository<UsersSites> userSites;
        private readonly IMediaService _mediaServices;
        private readonly IRepository<ReviewProduct> reviewProductRepopsitory;
        private readonly IRepository<ReplyMedia> _replyMediaRepository;
        private readonly IMapper mapper;
        private readonly IWorkContext workContext;

        public ReplyServices(IRepository<Reply> ReplyRepository,IRepository<Review> ReviewRepository ,
            IRepository<UserInteractionConfiguration> UserInteractionRepopsitory,
            IRepository<Order> OrderRepository,
            IRepository<User> UserRepository,
            IMediaService mediaServices,
                                    IRepository<UsersSites> UserSites,

            IRepository<ReviewProduct> ReviewProductRepopsitory,
               IRepository<ReplyMedia> replyMediaRepository


            , IMapper mapper, IWorkContext workContext)
        {
            replyRepository = ReplyRepository;
            reviewRepository = ReviewRepository;
            userInteractionRepopsitory = UserInteractionRepopsitory;
            orderRepository = OrderRepository;
            userRepository = UserRepository;
            userSites = UserSites;
            reviewProductRepopsitory = ReviewProductRepopsitory;
            this.mapper = mapper;
            this.workContext = workContext;
            _mediaServices=mediaServices;
            _replyMediaRepository=replyMediaRepository;
        }

        #region ReplyAdmin
        public async Task<bool> ChangeStatus(long id, long status)
        {
            try
            {
                var REPLAY = await replyRepository.Query().Where(x => x.Id == id).FirstOrDefaultAsync();

                if (REPLAY == null)
                {
                    throw new Exception("Not Found");
                }

                REPLAY.Status = (ReviewStatus)status;

                await replyRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<PaginationResult<ReplyViewModel>> Grid(SmartTableParam model)
        {
            try
            {
                var query = await replyRepository
                                             .Query()
                                             .Include(x=>x.User)
                                             .FilterReplay(model)
                                             .AsNoTrackingWithIdentityResolution()
                                             .ProjectTo<ReplyViewModel>(mapper.ConfigurationProvider)
                                             .OrderByDescending(x => x.Id)
                                             .PaginateAsync(model.Pagination.Number, model.Pagination.Start);
                return query;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }



        #endregion


        #region ReplayWeb
        public async Task<bool> Insert(ReplyForm model)
        {
            try
            {

                var userId = await workContext.GetCurrentUserId();
                var SiteId=workContext.GetCurrentSiteId();
                var UserSite = await userSites.Query().Where(x => x.UserId == userId && x.SiteId == SiteId).FirstOrDefaultAsync();

                if (!UserSite.CommentPermissions)
                {

                    throw new Exception("We regret to inform you that your commenting privileges have been temporarily suspended");

                }


                if (model.Status.HasValue && (model.Status == ReviewStatus.Deleted || model.Status == ReviewStatus.NotApproved))
                {
                    throw new Exception("Invalid review Status");
                }

                await CheckMaxReplay(model.ReviewId, userId);

                var reply = new Reply
                {
                    ReviewId = model.ReviewId,
                    UserId = userId,
                    Comment = model.Comment,
                    CreatedOn = DateTime.Now,
                    Status = model.Status.HasValue ? model.Status.Value : ReviewStatus.Pending
                };

                replyRepository.Add(reply);
                await replyRepository.SaveChangesAsync();
                var uploadedMediaIds = await UploadReplyMediaAsync(model.files, SiteId, userId);
                await InsertUpdateReplyMediasAsync(reply.Id, uploadedMediaIds);

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        public async Task<bool> Delete(long  replyId)
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();


                var userId = await workContext.GetCurrentUserId();

                var reply = replyRepository.Query()
                                             .Where(x => x.Id == replyId)
                                             .Include(x => x.Review)
                                             .ThenInclude(x => x.ReviewPosts)
                                             .ThenInclude(x => x.Post)
                                             .FirstOrDefault();



                if (reply is null)
                {
                    throw new Exception("Reply doesn't exist");
                }


                var postReviewd = reply.Review.ReviewPosts.Select(x => x.Post).FirstOrDefault();


                if ((postReviewd != null && postReviewd.CreatedById == userId) || reply.UserId == userId)
                {
                    reply.Status = ReviewStatus.Deleted;

                    replyRepository.Update(reply);
                    await replyRepository.SaveChangesAsync();

                }
                else
                {
                    throw new Exception("You don't have permission to remove reviews");
                }
                var reviewMedia = await _replyMediaRepository.Query().Where(x => x.ReplyId == reply.Id).ToListAsync();
                foreach (var media in reviewMedia)
                {
                    _replyMediaRepository.Remove(media);
                    await _mediaServices.DeleteMedia(media.Id);
                }
                await reviewRepository.SaveChangesAsync();




                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task CheckMaxReplay(long reviewId, long userId)
        {
            try
            {
                var ProductId =await reviewRepository.Query().Include(x=>x.ReviewProducts).Where(x => reviewId==x.Id).SelectMany(x=>x.ReviewProducts).Select(x=>x.ProductId).FirstOrDefaultAsync();

                var RoleIds = await userRepository.Query()
                                         .Include(x => x.UserRoles)
                                         .Where(c => c.UserRoles.Any(x => x.UserId == userId))
                                         .SelectMany(x => x.UserRoles)
                                         .Select(x => x.RoleId)
                                         .ToListAsync();
                var UserRole = RoleIds;
               


                var UserNotAdmin = UserRole.Any(x => x == 1);

                if (!UserNotAdmin)
                {
                    var Role = UserRole.FirstOrDefault();

                    var ReplyConfiguration = await userInteractionRepopsitory.Query().AsNoTracking().Where(x => x.ERoleTypes == (ERoleTypes)Role).FirstOrDefaultAsync();
                    if (ReplyConfiguration != null)
                    {
                        if (ReplyConfiguration.AllowCommnetIfPayOrder)
                        {

                            await CheckIfUserPayThisProduct(ProductId, userId);

                        }
                        await CheckIfMoreThanMax(ProductId, userId, ReplyConfiguration.MaxNumberOfReplays);
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

        private async Task CheckIfMoreThanMax(long ProductId,long UserId,int MaxReply)
        {
            try
            {

                var reviewProductbyUserId = await reviewProductRepopsitory.Query()
                                                                  .Include(x => x.Review)
                                                                  .ThenInclude(x => x.Replies)
                                                                  .Where(x => x.ProductId == ProductId)
                                                                  .SelectMany(x=>x.Review.Replies)
                                                                  .Where(x=>x.UserId==UserId)
                                                                  .CountAsync();

                if (MaxReply < reviewProductbyUserId+1)
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
        private async Task InsertUpdateReplyMediasAsync(long ReplyId, List<long>? uploadIds)
        {
            var ReplyMedia = await _replyMediaRepository.Query().Where(x => x.ReplyId == ReplyId).ToListAsync();
            _replyMediaRepository.RemoveRange(ReplyMedia);
            if (uploadIds.Count > 0)
            {
                var newsLetterMedias = uploadIds.Select(id => new ReplyMedia { ReplyId = ReplyId, MediaId = id }).ToList();
                _replyMediaRepository.AddRange(newsLetterMedias);
            }
            await _replyMediaRepository.SaveChangesAsync();
        }
        private async Task<List<long>> UploadReplyMediaAsync(List<IFormFile> files, long siteId, long userId)
        {
            if (files == null || !files.Any())
                return new List<long>();
            var allowedExtenions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var uploadedMediaIds = new List<long>();
            foreach (var file in files)
            {
                var extenion = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtenions.Contains(extenion))
                    throw new Exception("only imge files are allowed for Reply photos");

            }
            var uploaddModel = new MediaUploadModel
            {
                Uploads = files,
                ParentId = null,
                CreatedBy = userId,
                CultureId = 0,
                Force = false,
                ConvertToWebp = false,
                status = EStatus.Published
            };
            var savedMediaList = await _mediaServices.SaveMedia(uploaddModel);
            return savedMediaList.Select(x => x.Id).ToList();


        }
    }
}
