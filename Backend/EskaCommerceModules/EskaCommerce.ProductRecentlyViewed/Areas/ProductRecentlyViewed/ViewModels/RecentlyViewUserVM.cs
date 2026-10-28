using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.ProductRecentlyViewed.Areas.ProductRecentlyViewed.ViewModels
{
    public class RecentlyViewUserVM
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string CategorySlug { get; set; }
        
        public string ThuumbnailImageUrl { get; set; }
       
        public EskaCMS.Core.Areas.Core.ViewModels.Shared.CalculatedItemPrice Price { get; set; }

        public bool IsCallForPricing { get; set; }
        public bool HasVariations { get; set; }
        public bool IsAvaliable { get; set; }
        public string Slug { get; set; }
        public string PriceCurrency { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public bool InStock { get; set; }

        public double Rating { get; set; }
        public bool IsAvailable { get;  set; }
    }
}
