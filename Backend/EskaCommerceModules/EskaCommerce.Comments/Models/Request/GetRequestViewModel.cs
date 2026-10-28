using EskaCMS.Infrastructure.Web.SmartTable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Comments.Models.Request
{
    public class GetRequestViewModel
    {
        public string SearchText { get; set; }

        public long? ProductId { get; set; } 

        public PaginationRequestViewModel pagination { get; set; }
    }


    public class PaginationRequestViewModel
    {

        public int PageSize { get; set; } 
        public int PageNumber { get; set; } 


    }

    public class CommentRequestVM
    {
        public string SearchText { get; set; }

        public long? PostId { get; set; }

        public PaginationRequestViewModel pagination { get; set; }
    }
}
