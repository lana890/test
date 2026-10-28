using EskaCMS.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.Shippments.ShippingPrices.Services
{
    public class ShippingPrice
    {
        private readonly ICurrencyService _currencyService;

        public ShippingPrice(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        public string Name { get; set; }

        public decimal Price { get; set; }
        public string Description { get; set; }

        public string PriceText
        {
            get
            {
                return _currencyService.FormatCurrency(Price);
            }
        }
    }

}
