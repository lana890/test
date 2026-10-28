using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.ProductRecentlyViewed.Areas.ProductRecentlyViewed.ViewModels
{
    public class RecentlyRequestViewModel
    {
        public string SearchText { get; set; }

        public bool ByUserId { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}
