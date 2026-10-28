using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Addresses;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.ShippingPrices.Services
{
    public class GetShippingPriceRequest
    {
        public Address ShippingAddress { get; set; }

        public Address WarehouseAddress { get; set; }

        public IList<ShippingItem> ShippingItem { get; set; } = new List<ShippingItem>();

        public decimal OrderAmount { get; set; }
        public IList<CartItemVm> CartItems { get; set; } = new List<CartItemVm>();

    }
}
