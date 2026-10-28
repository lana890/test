using EskaCMS.Core.Services;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels
{
    public class AddToCartResult
    {
        private readonly ICurrencyService _currencyService;

        public AddToCartResult(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        public string ProductName { get; set; }
        public CartItemVm Item { get; set; }

        public string ProductImage { get; set; }

        public decimal ProductPrice { get; set; }

        public string VariationName { get; set; }

        public int Quantity { get; set; }

        public IEnumerable<ProductFeesVM> ProductAdditionalFees { get; set; }

      
        public int CartItemCount { get; set; }

        public decimal CartAmount { get; set; }

		public long CartId { get; set; }

		public string ProductPriceString =>  _currencyService.FormatByUserCurrency(ProductPrice);

        public string CartAmountString => _currencyService.FormatByUserCurrency(CartAmount);
    }

}
