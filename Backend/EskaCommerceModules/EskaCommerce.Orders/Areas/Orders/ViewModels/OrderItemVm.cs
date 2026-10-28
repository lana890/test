using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity.Catalog;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
    public class OrderItemVm
    {
        private readonly ICurrencyService _currencyService;

        public OrderItemVm(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        public long Id { get; set; }

      
        public string OrderCurrencyCode { get; set; }

        public long ProductId { get; set; }

        public string ProductName { get; set; }

        public DateTimeOffset? ProductDate { get; set; }

        public string ProductImage { get; set; }

        public decimal ProductPrice { get; set; }

        public int Quantity { get; set; }

        public int ShippedQuantity { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal TaxPercent { get; set; }

        public decimal DiscountAmount { get; set; }
        public string AdditionalDetails { get; set; }
        public string PlanName { get; set; }

        public decimal Total =>_currencyService.DcimalRound( Quantity * ProductPrice);

        public decimal TaxIncludedAmount => Total + TaxAmount;

        public decimal RowTotal => _currencyService.DcimalRound(Total + TaxAmount - DiscountAmount);

        public string TaxAmountString => _currencyService.FormatByUserCurrency(TaxAmount);

        public string ProductPriceString => _currencyService.FormatByUserCurrency(ProductPrice);

        public string DiscountAmountString => _currencyService.FormatByUserCurrency(DiscountAmount);

        public string TotalString => _currencyService.FormatByUserCurrency(Total);

        public string TaxIncludedAmountString => _currencyService.FormatByUserCurrency(TaxIncludedAmount);

        public string RowTotalString => _currencyService.FormatByUserCurrency(RowTotal);

        public IList<ProductFeesVM> ProductAdditionalFees { get; set; } = new List<ProductFeesVM>();
        public IList<ItemDetailAttribute> Attributes { get; set; } = new List<ItemDetailAttribute>();

        public IEnumerable<ProductVariationOptionVm> VariationOptions { get; set; } = new List<ProductVariationOptionVm>();
        public string QrCode { get;  set; }
    }


}
