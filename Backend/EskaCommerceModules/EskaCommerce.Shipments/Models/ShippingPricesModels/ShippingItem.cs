using EskaCMS.Entities.Entity.Catalog;
using System;
using System.Collections.Generic;
using System.Text;
using EskaCMS.Entities.Entity;
namespace EskaCommerce.ShippingPrices.Services
{
    public class ShippingItem
    {
        public long ProductId { get; set; }

        public Product Product { get; set; }

        public int Quantity { get; set; }
    }
}
