using System;
  using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Models;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;

namespace EskaCMS.Entities.Entity.Pricing
{
    public class CartRuleUsage : EntityBase
    {
        public CartRuleUsage()
        {
            CreatedOn = DateTimeOffset.Now;
        }

        public decimal DiscountAmount { get; set; }
        public long CartRuleId { get; set; }

        public CartRule CartRule { get; set; }

        public long? CouponId { get; set; }

        public Coupon Coupon { get; set; }

        public long UserId { get; set; }

        public User User { get; set; }

        public long? OrderId { get; set; }
        public Order Order { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
    }
}
