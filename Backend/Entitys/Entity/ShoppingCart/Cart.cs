using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Enum;
using EskaCMS.Entities.Entity.UserCatalog;

namespace EskaCMS.Entities.Entity.ShoppingCart
{
    public class Cart : EntityBase
    {
        public Cart()
        {
            CreatedOn = DateTimeOffset.Now;
            LatestUpdatedOn = DateTimeOffset.Now;
            IsActive = true;
        }

        public long CustomerId { get; set; }

        public User Customer { get; set; }

        public long CreatedById { get; set; }

		public string ReferenceId { get; set; }

		public User CreatedBy { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset LatestUpdatedOn { get; set; }

        public bool IsActive { get; set; }

        public bool LockedOnCheckout { get; set; }

        [StringLength(450)]
        public string CouponCode { get; set; }

        [StringLength(450)]
        public string CouponRuleName { get; set; }

        [StringLength(450)]
        public string ShippingMethod { get; set; }

        public bool IsProductPriceIncludeTax { get; set; }

        public decimal? ShippingAmount { get; set; }

        public decimal? TaxAmount { get; set; }
        public decimal RedeemAmount { get; set; }

        public IList<CartItem> Items { get; set; } = new List<CartItem>();

        /// <summary>
        /// Json serialized of shipping form
        /// </summary>
        public string ShippingData { get; set; }

        [StringLength(1000)]
        public string OrderNote { get; set; }

        public EskaCMS.Entities.Enum.GeneralEnums.CartTypes CartType { get; set; }

        public EskaCMS.Entities.Entity.Core.Site Site { get; set; }
        public long SiteId { get; set; }

        /// <summary>
        /// Json serialized of Delivery Option 
        /// </summary>

        public DeliveryOptionEnum ShippingDelivery { get; set; }
        public ICollection<Order> Orders { get; set; }
        public bool IsNotified { get; set; }
        public long? PlansVersionId { get; set; }
        public PlansVersions PlansVersion { get; set; }
    }

}
