using EskaCMS.Entities.Entity.Comments;
using EskaCMS.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Comments.Models.Response
{
    public class CommentWebViewModel
    {
        public long Id { get; set; }

        public string CommentText { get; set; }
        public CommentStatus Status { get; set; }

        public string CommenterName { get; set; }

        public DateTimeOffset CreationDate { get; set; }
        public IList<Comment> Replies { get; set; } = new List<Comment>();

    }
}
