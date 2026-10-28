
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class CustomBiddingTrackerVM
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Slug { get; set; }

        public decimal Price { get; set; }

        public decimal? SpecialForignPrice { get; set; }

        public decimal? SpecialPrice { get; set; }

        public bool IsCallForPricing { get; set; }

        public bool IsAllowToOrder { get; set; }

        public int? StockQuantity { get; set; }

        public DateTimeOffset? SpecialPriceStart { get; set; }

        public DateTimeOffset? SpecialPriceEnd { get; set; }

        public  Media ThumbnailImage { get; set; }

        public string ThumbnailUrl { get; set; }

        public int ReviewsCount { get; set; }

        public double? RatingAverage { get; set; }
        public decimal? CurrentBid { get; set; }
        public ERequestStatus Status { get; set; }
        public decimal? SellerBid { get; set; }
        public decimal? BuyerBid { get; set; }
        public DateTimeOffset? EndDate { get; set; }
    }
}
