using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.ProductRecentlyViewed.Areas.ProductRecentlyViewed.ViewModels
{
    public class MostViewedProducts
    {
        public long ProductId { get; set; }
        public string Name { get; set; }
        public int  Count { get; set; }
    }
}
