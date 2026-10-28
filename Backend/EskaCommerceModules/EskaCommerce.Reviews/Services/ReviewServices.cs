using AutoMapper;
using AutoMapper.QueryableExtensions;
using EskaCMS.Core.Events;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Reviews;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Media.Models;
using EskaCMS.Media.Services.Interfaces;
using EskaCommerce.Reviews.Areas.Reviews.ViewModels;
using EskaCommerce.Reviews.Extensions;
using EskaCommerce.Reviews.Models;
using EskaCommerce.Reviews.Models.ReplayModels;
using EskaCommerce.Reviews.Models.ReviewModels;
using EskaCommerce.Reviews.Models.ReviewModels.ReviewPosts;
using EskaCommerce.Reviews.Services.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Reviews.Services
{
    public class ReviewServices : IReviewServices
    {
        private readonly IRepository<Review> reviewRepository;
        private readonly IRepository<UserInteractionConfiguration> userInteractionRepopsitory;
        private readonly IRepository<User> userRepository;
        private readonly IRepository<Order> orderRepository;
        private readonly IRepository<ProductLink> productlinkRepository;
        private readonly IRepository<UsersSites> userSites;
        private readonly IWorkContext workContext;
        private readonly IMapper mapper;
        private readonly IMediator mediator;
        private readonly IRepository<ReviewProduct> reviewProductRepository;
        private readonly IRepository<ReviewPosts> reviewPostsRepository;
        private readonly IRestrictedWordsServices _restrictedWordsServices;
        private readonly IRepository<ReviewMedia> _reviewMediaRepository;
        private readonly IMediaService _mediaServices;

        public ReviewServices(IRepository<Review> ReviewRepository,
                        IRepository<UserInteractionConfiguration> UserInteractionRepopsitory,
                        IRepository<User> UserRepository,
                        IRepository<Order> OrderRepository,
                        IRepository<ProductLink> productlinkRepository,
                        IRepository<UsersSites> UserSites,
                         IRestrictedWordsServices restrictedWordsServices,
                          IWorkContext workContext, IMapper mapper
                        , IMediator mediator,
                        IRepository<ReviewProduct> ReviewProductRepository,
                        IRepository<ReviewPosts> ReviewPostsRepository,
                        IRepository<ReviewMedia> reviewMediaRepository,
                        IMediaService mediaService
            )
        {
            reviewRepository = ReviewRepository;
            userInteractionRepopsitory = UserInteractionRepopsitory;
            userRepository = UserRepository;
            orderRepository = OrderRepository;
            this.productlinkRepository = productlinkRepository;
            userSites = UserSites;
            this.workContext = workContext;
            this.mapper = mapper;
            this.mediator = mediator;
            reviewProductRepository = ReviewProductRepository;
            reviewPostsRepository = ReviewPostsRepository;
            _restrictedWordsServices = restrictedWordsServices;
            _reviewMediaRepository = reviewMediaRepository;
            _mediaServices = mediaService;
        }
        #region Admin

        public async Task<bool> BlockUserPermissions(long userId)
        {
            try
            {

                var Query = await userSites.Query().Where(x => x.UserId == userId && workContext.GetCurrentSiteId() == x.SiteId).FirstOrDefaultAsync();

                if (Query != null)
                {
                    Query.CommentPermissions = false;
                    
                }

                await  userSites.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<PaginationResult<UsersBlockedViewModel>> GetUserBlocked(UserBlockedViewModelRequest model)
        {
            try
            {
                var siteId=workContext.GetCurrentSiteId();

                var UsersBlocked= await userSites.Query()
                                                 .Where(x => x.SiteId == siteId &&x.CommentPermissions==false
                                                 &&(string.IsNullOrEmpty(model.Username)||x.User.UserName.Contains(model.Username))
                                                 ).Select(x=>new UsersBlockedViewModel { 
                                                 
                                                     Permissions="Blocked",
                                                     UserId=x.UserId,
                                                     Username=x.User.UserName
                                                 
                                                 
                                                 }).PaginateAsync(model.PageSize,model.PageNumber);

                return UsersBlocked;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> UnBlockUserPermissions(long userId)
        {
            try
            {

                var Query = await userSites.Query().Where(x => x.UserId == userId && workContext.GetCurrentSiteId() == x.SiteId).FirstOrDefaultAsync();

                if (Query != null)
                {
                    Query.CommentPermissions = true;

                }

                await userSites.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<bool> ChangeStatus(long id, long status)
        {
            try
            {
                var query = await reviewRepository.Query().Where(x => x.Id == id).FirstOrDefaultAsync();

                if (query == null)
                {
                    throw new Exception("Not Found");
                }


                query.Status = (ReviewStatus)status;

                await reviewRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<List<ReviewDetailsViewModel>> GetProductReviewer(long productId)
        {
            try
            {
                var query = await reviewProductRepository.Query().Include(x => x.Product)
                                                   .Include(x => x.Review)
                                                   .ThenInclude(x => x.Replies)
                                                   .AsNoTrackingWithIdentityResolution()
                                                   .Where(x => x.ProductId == productId&&x.Review.Status==ReviewStatus.Approved)
                                                   .Select(x => x.Review)
                                                   .ProjectTo<ReviewDetailsViewModel>(mapper.ConfigurationProvider)
                                                   .ToListAsync();

                return query;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<PaginationResult<ReviewViewModel>> Grid(SmartTableParam model)
        {
            try
            {
                var query = await reviewRepository
                                             .Query()
                                             .Include(x => x.User)
                                             .Include(x => x.ReviewProducts)
                                             .ThenInclude(x => x.Product)
                                             .ThenInclude(x=>x.ProductDetails)
                                             .Include(x=>x.ReviewPosts)
                                             .ThenInclude(x=>x.Post)
                                             .Include(x => x.Replies)
                                             .AsNoTrackingWithIdentityResolution()
                                             .FilterReview(model)
                                             .ProjectTo<ReviewViewModel>(mapper.ConfigurationProvider)
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


        #region WebProducts
        public async Task<bool> AddReview(ReviewForm model,List<IFormFile> files)
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();

                var userId = await workContext.GetCurrentUserId();

                var UserSite = await userSites.Query().Where(x => x.UserId == userId && x.SiteId == SiteId).FirstOrDefaultAsync();

                if (!UserSite.CommentPermissions)
                {

                    throw new Exception("We regret to inform you that your commenting privileges have been temporarily suspended");
                
                }

                if (model.ProductId == 0)
                {
                    throw new Exception("Please Choose the Product");

                }

                if (model.Rating >= 6)
                {
                    throw new Exception("You Can Not Rating More Than 5");

                }

                if (await _restrictedWordsServices.IsRestrictedWord(model.Comment, SiteId) || await _restrictedWordsServices.IsRestrictedWord(model.Title, SiteId))
                {
                    throw new Exception("This comment have contains vilotion words");
                }

                if (model.Status.HasValue && (model.Status == ReviewStatus.Deleted || model.Status == ReviewStatus.NotApproved))
                {
                    throw new Exception("Invalid review Status");
                }
                const int MaxReviewMediaCount = 5;
                if (files?.Count > MaxReviewMediaCount)
                    throw new Exception($"You can attach up to {MaxReviewMediaCount} photos");
                await CheckMaxReview(model.ProductId, userId);

                var review = new Review
                {
                    Rating = model.Rating,
                    Title = model.Title,
                    Comment = model.Comment,
                    UserId = userId,
                    Status = model.Status.HasValue ? model.Status.Value : ReviewStatus.Pending,
                    SiteId = SiteId 
                };

                reviewRepository.Add(review);
                await reviewRepository.SaveChangesAsync();
                var uploadedMediaIds = await UploadReviewMediaAsync(files, SiteId, userId);
                await InsertUpdateReviewMediasAsync(review.Id, uploadedMediaIds);
                 
                await MappingToProducts(model.ProductId, review.Id);

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        

        public async Task<ReviewVm> List(long ProductId, int PageSize, int PageNumber)
        {
            try
            {
                var model = new ReviewVm();
                var MainProduct =await GetMainProduct(ProductId);
                var query = reviewRepository
                    .Query()
                    .AsNoTrackingWithIdentityResolution()
                    .Include(x => x.ReviewProducts)
                    .ThenInclude(x => x.Product)
                    .Include(x => x.Replies)
            
                    .Where(x => x.ReviewProducts.Any(x => x.MainProductId == MainProduct&& x.Product.Status==EStatus.Published) && (x.Status == EskaCMS.Entities.Entity.ReviewStatus.Approved))
                    .OrderByDescending(x => x.CreatedOn)
                    .ProjectTo<ReviewItem>(mapper.ConfigurationProvider);

                model.Items = await query.PaginateAsync(PageSize, PageNumber);

                // Only the Rating field is needed for the counts/average below, so fetch just the
                // ratings instead of re-materializing every Review (with its ReviewProducts/Product/
                // Replies graph) a second time. Same filter, same values — far less RAM and CPU.
                var allRatings = await query.Select(x => x.Rating).ToListAsync();

                model.ReviewsCount = allRatings.Count;
                model.Rating1Count = allRatings.Count(x => x == 1);
                model.Rating2Count = allRatings.Count(x => x == 2);
                model.Rating3Count = allRatings.Count(x => x == 3);
                model.Rating4Count = allRatings.Count(x => x == 4);
                model.Rating5Count = allRatings.Count(x => x == 5);

                if (allRatings.Count > 0)
                {
                    model.RatingAverage = allRatings.Average(x => x);

                }

                return model;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        #endregion


        #region WebPosts
         public async Task<bool> AddReviewPosts(ReviewPostVM model)
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();

                var userId = await workContext.GetCurrentUserId();

                var UserSite = await userSites.Query().Where(x => x.UserId == userId && x.SiteId == SiteId).FirstOrDefaultAsync();

                if (!UserSite.CommentPermissions)
                {

                    throw new Exception("We regret to inform you that your commenting privileges have been temporarily suspended");

                }

                if (model.PostId == 0)
                {
                    throw new Exception("Please Choose the Post");

                }

                if (model.Rating >= 6)
                {
                    throw new Exception("You Can Not Rating More Than 5");

                }

                if(await _restrictedWordsServices.IsRestrictedWord(model.Comment,SiteId)||await _restrictedWordsServices.IsRestrictedWord(model.Title, SiteId))
                {
                    throw new Exception("This comment have contains vilotion words");
                }


                if (model.Status.HasValue && (model.Status == ReviewStatus.Deleted || model.Status == ReviewStatus.NotApproved))
                {
                    throw new Exception("Invalid review Status");
                }



                await CheckMaxReviewPosts(model.PostId, userId);

                var review = new Review
                {
                    Rating = model.Rating,
                    Title = model.Title,
                    Comment = model.Comment,
                    UserId = userId,
                    Status = model.Status.HasValue ? model.Status.Value : ReviewStatus.Pending,
                    SiteId = SiteId
                };

                reviewRepository.Add(review);
                await reviewRepository.SaveChangesAsync();

                await MappingToPosts(model.PostId, review.Id);

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }       
        
        }



        public async Task<bool> RemoveReview(long Id)
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();


                var userId = await workContext.GetCurrentUserId();

                var review = reviewRepository.Query()
                                             .Where(x => x.Id == Id)
                                             .Include(x => x.ReviewPosts)
                                             .ThenInclude(x=>x.Post)
                                             .FirstOrDefault();


              
                if(review is null)
                {
                    throw new Exception("Review doesn't exist");
                }


                var postReviewd = review.ReviewPosts.Select(x=>x.Post).FirstOrDefault();


                if((postReviewd != null && postReviewd.CreatedById == userId) || review.UserId == userId)
                {
                    review.Status = ReviewStatus.Deleted;

                    reviewRepository.Update(review);
                    await reviewRepository.SaveChangesAsync();
                   var reviewMedia= await _reviewMediaRepository.Query().Where(x => x.ReviewId == review.Id).ToListAsync();
                    foreach(var media in reviewMedia)
                    {
                        _reviewMediaRepository.Remove(media);
                        await _mediaServices.DeleteMedia(media.Id);
                    }
                    await reviewRepository.SaveChangesAsync();

                }
                else
                {
                    throw new Exception("You don't have permission to remove reviews");
                }

              


                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public async Task<ReviewPostsVm> ListPosts(long PostId, int PageSize, int PageNumber)
        {
            try
            {
                var model = new ReviewPostsVm();

                var query = reviewRepository
                    .Query()
                    .AsNoTrackingWithIdentityResolution()
                    .Include(x => x.ReviewPosts)
                    .ThenInclude(x => x.Post)
                    .Include(x => x.Replies)
                    .Where(x => x.ReviewPosts.Any(x => x.PostId == PostId) && (x.Status == EskaCMS.Entities.Entity.ReviewStatus.Approved))
                    .OrderByDescending(x => x.CreatedOn)
                    .ProjectTo<ReviewPostItem>(mapper.ConfigurationProvider);

                model.Items = await query.PaginateAsync(PageSize, PageNumber);

                // Only Rating is needed for the counts/average — fetch just the ratings instead of
                // re-materializing every Review (with ReviewPosts/Post/Replies) a second time.
                var allRatings = await query.Select(x => x.Rating).ToListAsync();

                model.ReviewsCount = allRatings.Count;

                model.Rating1Count = allRatings.Count(x => x == 1);

                model.Rating2Count = allRatings.Count(x => x == 2);

                model.Rating3Count = allRatings.Count(x => x == 3);

                model.Rating4Count = allRatings.Count(x => x == 4);

                model.Rating5Count = allRatings.Count(x => x == 5);

                if (allRatings.Count > 0)
                {

                    model.RatingAverage = allRatings.Average(x => x);

                }

                return model;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        #endregion


        #region HelpersProducts

        private async Task CheckMaxReview(long ProductId, long UserId)
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
                    var ReviewConfiguration = await userInteractionRepopsitory.Query().Where(x => x.ERoleTypes == (ERoleTypes)Role).FirstOrDefaultAsync();

                    if (ReviewConfiguration != null)
                    {
                        if (ReviewConfiguration.AllowCommnetIfPayOrder)
                        { 
                            await CheckIfUserPayThisProduct(ProductId, UserId);
                        }
                        await CheckIfMoreThanMax(ProductId, UserId, ReviewConfiguration.MaxNumberOfReviews);
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
        private async Task CheckIfMoreThanMax(long ProductId, long UserId,int MaxReview)
        {
            try
            {
                var NumberOfReviewr = await reviewProductRepository.Query().Include(x => x.Review)
                                                                .Where(x => x.ProductId == ProductId)
                                                                .Select(x => x.Review)
                                                                .Where(x => x.UserId == UserId)
                                                                .CountAsync();

                if (MaxReview < NumberOfReviewr+1)
                {
                    throw new Exception("Comment limit reached");
                }



            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private async Task MappingToProducts(long productId, long id)
        {
            try
            {

                var mainproduct = await productlinkRepository.Query().AsNoTracking().Where(x => x.LinkedProductId == productId).FirstOrDefaultAsync();

                var query = new ReviewProduct { ProductId = productId, ReviewId = id, MainProductId = mainproduct != null ?mainproduct.ProductId: productId };


                reviewProductRepository.Add(query);
                await reviewProductRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private async Task<long?> GetMainProduct(long productId)
        {
            try
            {

                var mainproduct = await productlinkRepository.Query().AsNoTracking().Where(x => x.LinkedProductId == productId).Select(x=>x.ProductId).FirstOrDefaultAsync();
                return mainproduct;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        #endregion

        #region HelpersPosts

        private async  Task MappingToPosts(long postId, long id)
        {
            try
            {
                var query = new ReviewPosts { PostId = postId, ReviewId = id };

                reviewPostsRepository.Add(query);
                await reviewPostsRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private async Task CheckMaxReviewPosts(long PostId, long UserId)
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
                    var ReviewConfiguration = await userInteractionRepopsitory.Query().Where(x => x.ERoleTypes == (ERoleTypes)Role).FirstOrDefaultAsync();

                    if (ReviewConfiguration != null)
                    {
                        await CheckIfMoreThanMaxPosts(PostId, UserId, ReviewConfiguration.MaxNumberOfReviews);
                    }
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }


        }
        private async Task CheckIfMoreThanMaxPosts(long PostId, long UserId, int MaxReview)
        {
            try
            {
                var NumberOfReviewr = await reviewPostsRepository.Query().Include(x => x.Review)
                                                                .Where(x => x.PostId == PostId)
                                                                .Select(x => x.Review)
                                                                .Where(x => x.UserId == UserId)
                                                                .CountAsync();

                if (MaxReview < NumberOfReviewr + 1)
                {
                    throw new Exception("Comment limit reached");
                }



            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private async Task InsertUpdateReviewMediasAsync(long ReviewId, List<long>? uploadIds)
        {
            var reviewMedia = await _reviewMediaRepository.Query().Where(x => x.ReviewId == ReviewId).ToListAsync();
            _reviewMediaRepository.RemoveRange(reviewMedia);
            if (uploadIds.Count > 0)
            {
                var newsLetterMedias = uploadIds.Select(id => new ReviewMedia { ReviewId = ReviewId, MediaId = id }).ToList();
                _reviewMediaRepository.AddRange(newsLetterMedias);
            }
            await _reviewMediaRepository.SaveChangesAsync();
        }

        private async Task<List<long>> UploadReviewMediaAsync(List<IFormFile> files,long siteId,long userId)
        {
            if (files == null || !files.Any())
                return new List<long>();
            var allowedExtenions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var uploadedMediaIds=new List<long>();
            foreach (var file in files)
            {
                var extenion = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtenions.Contains(extenion))
                    throw new Exception("only imge files are allowed for review photos");

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
                    return savedMediaList.Select(x=>x.Id).ToList();
                
            
        }
        #endregion

    }
}