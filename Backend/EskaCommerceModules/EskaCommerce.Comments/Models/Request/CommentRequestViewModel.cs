using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Comments.Models.Request
{
    public class CommentRequestViewModel
    {
        public string Comment { get; set; } 
        public long? ParentId { get; set; } 
        public long ProductId { get; set; }
        

    }

    public class CommentPostsRequestViewModel
    {
        public string Comment { get; set; }
        public long? ParentId { get; set; }
        public long PostId { get; set; }


    }
}
