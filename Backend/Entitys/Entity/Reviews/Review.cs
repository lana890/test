using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Infrastructure.Models;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Comments;

namespace EskaCMS.Entities.Entity.Reviews
{
    public class Review : EntityBase
    {
        public Review()
        {
            Status = ReviewStatus.Pending;
            CreatedOn = DateTimeOffset.Now;
        }

        public long UserId { get; set; }

        public User User { get; set; }

        [StringLength(450)]
        public string Title { get; set; }

        public IList<ReviewProduct>  ReviewProducts { get; set; }
        public IList<ReviewPosts> ReviewPosts { get; set; }
        public string Comment { get; set; }

        public int Rating { get; set; }

        public ReviewStatus Status { get; set; }


        public DateTimeOffset CreatedOn { get; set; }
        public IList<Reply> Replies { get; protected set; } = new List<Reply>();
        public IList<ReviewMedia> ReviewMedia= new List<ReviewMedia>();
        public EskaCMS.Entities.Entity.Core.Site Site { get; set; }
        public long SiteId { get; set; }

    }
}
