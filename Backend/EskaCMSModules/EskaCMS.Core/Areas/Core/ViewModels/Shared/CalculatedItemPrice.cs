using EskaCMS.Core.Services;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EskaCMS.Core.Areas.Core.ViewModels.Shared
{
    public class CalculatedItemPrice
    {
        private ICurrencyService _currencyService;

        public CalculatedItemPrice(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        public decimal Price { get; set; }

        public decimal? SpecialPrice { get; set; }

        [NotMapped]
        public string Tax { get; set; } = "0.00%";
        [NotMapped]
        public string PriceWithTax { get; set; } = null;

        [NotMapped]
        public string SpecialPriceWithTax { get; set; } = null;
        public string PriceCurrency { get; set; }

        public int PercentOfSaving { get; set; }

        public string PriceString =>( _currencyService.DcimalRound(Price)).ToString();
        public string PriceOriginalString { get; set; }
        public DateTimeOffset? OfferEndsAt { get; set; }

        public string SpecialPriceString => SpecialPrice.HasValue ? (_currencyService.DcimalRound(SpecialPrice.Value)).ToString() : string.Empty;
    }
}
