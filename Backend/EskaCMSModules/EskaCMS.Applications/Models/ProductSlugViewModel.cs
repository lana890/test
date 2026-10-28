using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Sites.Models
{
    public class ProductSlugViewModel
    {
                public bool IsCallForPricing { get; set; }
        public bool IsAllowToOrder { get; set; }
        public bool StockTrackingIsEnabled { get; set; }
        public string SKU { get; set; }
        public int ReviewsCount { get; set; }
        public double? RatingAverage { get; set; }
        public List<long>? MediaIds { get; set; }
        public List<string> ImagesUrl { get; set; }
    }
}
