using System;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Infrastructure.Models;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Reviews
{
    public class Reply : EntityBase
    {
        public Reply()
        {
            Status = ReviewStatus.Pending;
            CreatedOn = DateTimeOffset.Now;
        }

        public long ReviewId { get; set; }

        public Review Review { get; set; }

        public long UserId { get; set; }

        public User User { get; set; }

        public string Comment { get; set; }

        public ReviewStatus Status { get; set; }

        public DateTimeOffset CreatedOn { get; set; }
        public IList<ReplyMedia> ReplyMedia { get; set; } = new List<ReplyMedia>();

    }
}
