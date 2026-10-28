using EskaCommerce.Shippments.ShippingPrices.Services;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
    public class OrderTaxAndShippingPriceVm
    {
        public bool IsProductPriceIncludedTax { get; set; }

        public IList<ShippingPrice> ShippingPrices { get; set; }

        public string SelectedShippingMethodName { get; set; }
        public bool IsSuccess { get; set; }
        public CartVm Cart { get; set; }
    }
}
