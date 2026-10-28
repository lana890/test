using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Sites.Models
{
    public class ProductLinkSlugViewModel
    {
        public string Slug { get; set; }
        public decimal oldPrice { get; set; }
        public decimal SpeacialPrice { get; set; }
        public bool IsAllowToOrder { get; set; }
        public bool IsCallForPricing { get; set; }
        public string ThumbnailImageUrl { get; set; }

        public long ProductLinkId { get; set; }
        public string NameProductLink { get; set; }

    }
}
