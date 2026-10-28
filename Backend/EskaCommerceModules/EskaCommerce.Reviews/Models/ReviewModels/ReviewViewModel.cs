using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Reviews;
using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using EskaCMS.Entities.Entity.Comments;
using EskaCommerce.Reviews.Models.ReviewModels.ReviewPosts;

namespace EskaCommerce.Reviews.Models
{
    public class ReviewViewModel
    {
        public long Id { get; set; }

        public string Title { get; set; }

        public string Comment { get; set; }

        public string EntityName { get; set; }
        public int Rating { get; set; }
        public IList<ProductReviewer> Products { get; set; } = new List<ProductReviewer>();
        public IList<PostsReviewer> Posts { get; set; } = new List<PostsReviewer>();
        public IList<ReviewViewMedia> Medias { get; set; } = new List<ReviewViewMedia>();

        public long UserId { get; set; }
        public string ReviewerName { get; set; }
        public DateTimeOffset CreatedOn { get; set; }

        public EskaCMS.Entities.Entity.ReviewStatus Status { get; set; }


    }

    public class ReviewViewMedia
    {
        public long Id { get; set;}
        public string URL { get; set; }
        public string ContentType {  get; set; }
    }
    public class ReviewDetailsViewModel
    {
        public long Id { get; set; }

        public string ReviewerName { get; set; }

        public string Title { get; set; }

        public string Comment { get; set; }

        public int Rating { get; set; }

        public ReviewStatus Status { get; set; }


        public DateTimeOffset CreatedOn { get; set; }
        public IList<Reply> Replies { get;  set; } = new List<Reply>();
        public IList<ReviewViewMedia> Medias { get; set; } = new List<ReviewViewMedia>();

    }


    public class ProductReviewer
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; }
    }
}