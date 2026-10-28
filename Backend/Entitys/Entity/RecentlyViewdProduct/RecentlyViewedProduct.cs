using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Models;
using System;
using System.ComponentModel;

namespace EskaCMS.Entities.Entity.ProductRecentlyViewed
{
    public class RecentlyViewedProduct : EntityBase
    {
        public long UserId { get; set; }

        public User User { get; set; }
        public long ProductId { get; set; }
        public Product Product { get; set; }
        [DefaultValue(0)]
        public SourceTraffic? Source { get; set; }
        public DateTimeOffset LatestViewedOn { get; set; }
    }
}
