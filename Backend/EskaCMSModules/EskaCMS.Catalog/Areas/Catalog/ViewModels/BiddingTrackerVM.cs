using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class BiddingTrackerVM
    {
        public long Id { get; set; }
        public decimal BiddingAmount { get; set; }
        public long BuyerId { get; set; }
        public string BuyerName { get; set; }
        public long CreatedById { get; set; }
        public long SellerId { get; set; }
        public string SellerName { get; set; }
        public long AuctionId { get; set; }
        public DateTimeOffset CreationDate { get; set; }
    }
}
