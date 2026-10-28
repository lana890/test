using EskaCMS.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Comments.Models.Response
{
    public class CommentAdminViewModel
    {
        public long Id { get; set; }

        public string CommentText { get; set; }
        public string Type { get; set; }
        public CommentStatus Status { get; set; }

        public string CommenterName { get; set; }
        public long UserId { get; set; }

        public DateTimeOffset CreationDate { get; set; } 


    }
}
