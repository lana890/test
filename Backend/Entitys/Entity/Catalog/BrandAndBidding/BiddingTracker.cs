

using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Text;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class BiddingTracker : EntityBase
    {
        public decimal BiddingAmount { get; set; }
        public long BuyerId { get; set; }
        public User Buyer { get; set; }
        public long CreatedById { get; set; }
        public User CreatedBy { get; set; }
        public long SellerId { get; set; }
        public User Seller { get; set; }
        public long AuctionId { get; set; }
        public Product Auction { get; set; }
        public DateTimeOffset CreationDate { get; set; }
    }
}
