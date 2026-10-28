using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Text;
using EskaCMS.Entities.Entity.Catalog;
using System.Text.Json.Serialization;

namespace EskaCMS.Entities.Entity.Orders
{
    public class OrderItem : EntityBase
    {
        [JsonIgnore]
        public Order Order { get; set; }

        public long ProductId { get; set; }

        [JsonIgnore]
        public Product Product { get; set; }

        public decimal ProductPrice { get; set; }
        public decimal? ProductCost { get; set; }


        public int Quantity { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal TaxPercent { get; set; }
        public string AdditionalDetails { get; set; }

        public string QrCode { get; set; }
    }

}
