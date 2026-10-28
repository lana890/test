using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Reviews;
using EskaCMS.Entities.Entity.Catalog;

namespace EskaCMS.Entities.Entity.Comments
{
    public class Comment : EntityBase
    {
        public Comment()
        {
            Status = Enum.CommentStatus.Approved;
            CreatedOn = DateTimeOffset.UtcNow;
        }

        public long UserId { get; set; }

        public User User { get; set; }
        public string CommentText { get; set; }

        public Enum.CommentStatus Status { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public long? ParentId { get; set; }

        public Comment Parent { get; set; }

        public IList<Comment> Replies { get; protected set; } = new List<Comment>();
        public IList<CommentProduct>   CommentProducts { get;  set; } 

    }
}
