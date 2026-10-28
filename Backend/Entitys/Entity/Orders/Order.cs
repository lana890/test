
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Entities.Entity.Pricing;
using EskaCMS.Entities.Entity.Shipments;
using EskaCMS.Entities.Entity.UserCatalog;
using EskaCMS.Entities.Entity.Inventory;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections;
using System.Text.Json.Serialization;

namespace EskaCMS.Entities.Entity.Orders
{
    public class Order : EntityBase
    {
        public Order()
        {
            CreatedOn = DateTimeOffset.Now;
            LatestUpdatedOn = DateTimeOffset.Now;
            OrderStatus = OrderStatus.New;
            IsMasterOrder = false;
        }

        public long CustomerId { get; set; }


        public long SiteId { get; set; }
        public EskaCMS.Entities.Entity.Core.Site Site { get; set; }

        [JsonIgnore] // To simplify the json stored in order history
        public User Customer { get; set; }

        public DateTimeOffset LatestUpdatedOn { get; set; }

        public long LatestUpdatedById { get; set; }

        [JsonIgnore]
        public User LatestUpdatedBy { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public long CreatedById { get; set; }

        [JsonIgnore]
        public User CreatedBy { get; set; }

        public long? VendorId { get; set; }

        [StringLength(450)]
        public string CouponCode { get; set; }

        [StringLength(450)]
        public string CouponRuleName { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal RedeemAmount { get; set; }

        public decimal SubTotal { get; set; }

        public decimal SubTotalWithDiscount { get; set; }

        public long? ShippingAddressId { get; set; }

        public OrderAddress ShippingAddress { get; set; }

        public long? BillingAddressId { get; set; }

        public OrderAddress BillingAddress { get; set; }

        public IList<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public List<ProductSerial> ProductSerials { get; set; } = new List<ProductSerial>();
        public List<Shipment> Shipment { get; set; } = new List<Shipment>();



        public OrderStatus OrderStatus { get; set; }

        [StringLength(1000)]
        public string OrderNote { get; set; }

        public long? ParentId { get; set; }

        [JsonIgnore]
        public Order Parent { get; set; }

        public bool IsMasterOrder { get; set; }

        [StringLength(450)]
        public string ShippingMethod { get; set; }

        public decimal ShippingFeeAmount { get; set; }
        public decimal ShippingDiscountAmount { get; set; }


        public decimal TaxAmount { get; set; }

        public decimal OrderTotal { get; set; }

        [StringLength(450)]
        public string PaymentMethod { get; set; }

        public decimal PaymentFeeAmount { get; set; }

        public string CurrencyCode { get; set; }
        public long? CartId { get; set; }
        public Cart Cart { get; set; }
        public string OrderReferanceId { get; set; }
        public IList<CartRuleUsage> CartRuleUsage { get; set; }
        public long? PlansVersionId { get; set; }
        public PlansVersions PlansVersion { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.EPlatformSource Channel { get; set; }  // this feild named by firas 
        public IList<Order> Children { get; protected set; } = new List<Order>();
        public void AddOrderItem(OrderItem item)
        {
            item.Order = this;
            OrderItems.Add(item);
        }

        public long WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; }
        public string CurrencyCodeCustomer { get; set; }

        public decimal CustomerCurrencyOrderTotal { get; set; }
        public bool IsProcessed { get; set; }
        public IList<UserPlans>  UserPlans { get; set; }
        public IList<OrderAdditionalFees> OrderAdditionalFees { get; set; }

    }

}
