using AutoMapper;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.BlogArchive;
using EskaCMS.Entities.Entity.Reviews;
using EskaCommerce.Reviews.Areas.Reviews.ViewModels;
using EskaCommerce.Reviews.Models;
using EskaCommerce.Reviews.Models.ReplayModels;
using EskaCommerce.Reviews.Models.ReviewModels.ReviewPosts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Reviews.Mapping
{
    public class ProfileReviewAndReply : Profile
    {
        public ProfileReviewAndReply()
        {
            CreateMap<Reply, ReplyViewModel>()
                .ForMember(x => x.UserId, r => r.MapFrom(d => d.UserId))
                .ForMember(x => x.ReplierName, r => r.MapFrom(d => d.User.FullName))
                .ForMember(x => x.EntityName, r => r.MapFrom(d => d.Review.ReviewProducts.Any() ? d.Review.ReviewProducts.Select(x => x.Product).SelectMany(x => x.ProductDetails).OrderByDescending(x => x.CultureId).FirstOrDefault().Name : d.Review.ReviewPosts.Select(x => x.Post).OrderByDescending(x => x.CultureId).FirstOrDefault().Title))
                .ForMember(x => x.Medias, r => r.MapFrom(d => d.ReplyMedia.Count == 0 ? new List<ReplyViewMedia>() : d.ReplyMedia.Select(rm => new ReplyViewMedia
                {
                    Id = rm.Id,
                    URL = rm.Media.PublicUrl,
                    ContentType = rm.Media.ContentType
                }).ToList()));

            CreateMap<Review, ReviewViewModel>()
            .ForMember(x => x.ReviewerName, r => r.MapFrom(d => d.User.FullName))
            .ForMember(x => x.UserId, r => r.MapFrom(d => d.UserId))
             .ForMember(x => x.EntityName, r => r.MapFrom(d => d.ReviewProducts.Any() ? d.ReviewProducts.Select(x => x.Product).SelectMany(x => x.ProductDetails).OrderByDescending(x => x.CultureId).FirstOrDefault().Name : d.ReviewPosts.Select(x => x.Post).OrderByDescending(x => x.CultureId).FirstOrDefault().Title))
            .ForMember(x => x.Products, r => r.MapFrom(d => d.ReviewProducts.Count == 0 ? new List<ProductReviewer>() : d.ReviewProducts.Select(x => x.Product).SelectMany(x => x.ProductDetails).OrderByDescending(x => x.CultureId).Select(x => new ProductReviewer { ProductId = x.ProductId, ProductName = x.Name }).Take(1).ToList()))
            .ForMember(x => x.Posts, r => r.MapFrom(d => d.ReviewPosts.Count == 0 ? new List<PostsReviewer>() : d.ReviewPosts.Select(x => x.Post).OrderByDescending(x => x.CultureId).Select(x => new PostsReviewer { PostId = x.Id, PostName = x.Title }).Take(1).ToList()))
            .ForMember(x => x.Medias, r => r.MapFrom(d => d.ReviewMedia.Count == 0 ? new List<ReviewViewMedia>() : d.ReviewMedia.Select(rm => new ReviewViewMedia
            {
                Id = rm.Id,
                URL = rm.Media.PublicUrl,
                ContentType = rm.Media.ContentType
            }).ToList()));
            
            CreateMap<EskaCommerce.Reviews.Models.ReviewModels.ReviewPosts.PostsReviewer, EskaCMS.Entities.Entity.Reviews.ReviewPosts>();

            CreateMap<Review, ReviewItem>()
            .ForMember(x => x.ReviewerName, r => r.MapFrom(d => d.User.FullName))
                .ForMember(x => x.Replies, r => r.MapFrom(d => d.Replies
                .Where(r => r.Status == EskaCMS.Entities.Entity.ReviewStatus.Approved)
                .OrderByDescending(r => r.CreatedOn)
                .Select(r => new ReplyViewModel
                {
                    Comment = r.Comment,
                    ReplierName = r.User.FullName,
                    CreatedOn = r.CreatedOn,
                    Status = r.Status,
                    ReviewId = r.ReviewId,
                    UserId = r.UserId,
                    Id = r.Id,
                    Medias = r.ReplyMedia.Count == 0 ? new List<ReplyViewMedia>() : r.ReplyMedia.Select(rm => new ReplyViewMedia
                    {
                        Id = rm.Id,
                        URL = rm.Media.PublicUrl,
                        ContentType = rm.Media.ContentType
                    }).ToList()
                })
                .ToList()))

            .ForMember(x => x.Products, r => r.MapFrom(f=>f.ReviewProducts.Select(x => new ProductReviewer
            {
                ProductId = x.ProductId,
                ProductName = x.Product.ProductDetails.FirstOrDefault().Name

            }).ToList()))
            .ForMember(x => x.Medias, r => r.MapFrom(d => d.ReviewMedia.Count == 0 ? new List<ReviewViewMedia>() : d.ReviewMedia.Select(rm => new ReviewViewMedia
            {
                Id = rm.Id,
                URL = rm.Media.PublicUrl,
                ContentType = rm.Media.ContentType
            }).ToList()));

            CreateMap<Review, ReviewDetailsViewModel>()
           .ForMember(x => x.ReviewerName, r => r.MapFrom(d => d.User.FullName));


            CreateMap<Review, ReviewPostItem>()
           .ForMember(x => x.ReviewerName, r => r.MapFrom(d => d.User.FullName))
           .ForMember(x => x.Replies, r => r.MapFrom(d => d.Replies
                           .Where(r => r.Status == ReviewStatus.Approved)
                           .OrderByDescending(r => r.CreatedOn)
                           .Select(r => new ReplyViewModel
                           {
                               Comment = r.Comment,
                               ReplierName = r.User.FullName,
                               CreatedOn = r.CreatedOn,
                               Status=r.Status,
                               ReviewId=r.ReviewId,
                               Id=r.Id,
                               UserId=r.UserId
                               
                           })
                           .ToList()))
            .ForMember(x => x.UserId, r => r.MapFrom(d => d.UserId))

           .ForMember(x => x.Posts, r => r.MapFrom(f => f.ReviewPosts.Select(x => new PostsReviewer
           {
               PostId = x.Id,
               PostName = x.Post.Title

           }).ToList())).ForMember(x => x.Medias, r => r.MapFrom(d => d.ReviewMedia.Count == 0 ? new List<ReviewViewMedia>() : d.ReviewMedia.Select(rm => new ReviewViewMedia
           {
               Id = rm.Id,
               URL = rm.Media.PublicUrl,
               ContentType = rm.Media.ContentType
           }).ToList()));

        }
    }
}
