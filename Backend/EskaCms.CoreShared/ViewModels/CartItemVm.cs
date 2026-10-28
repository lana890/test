using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Services;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Entities.Entity.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.UserCatalog;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels
{
    public class CartItemVm
    {
        private readonly ICurrencyService _currencyService;

        public CartItemVm(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        public long Id { get; set; }

        public long ProductId { get; set; }

        public string ProductName { get; set; }


        public bool IsSerailProduct { get; set; }
        public string ProductImage { get; set; }

        public decimal ProductPrice { get; set; }
        public decimal ProductPriceWithDiscount { get; set; }


        public long? TaxClassId { get; set; }
        public bool? InStock { get; set; }

        public decimal TaxAmount { get; set; }
        public string TaxPercentage { get; set; }
        public string ProductPriceStringWithDiscountString => _currencyService.FormatByUserCurrency(ProductPriceWithDiscount);

        public string ProductPriceString => _currencyService.FormatByUserCurrency(ProductPrice);
        public int ProductStockQuantity { get; set; }
        public bool ProductStockTrackingIsEnabled { get; set; }

        public bool IsProductAvailabeToOrder { get; set; }

        public int Quantity { get; set; }


        public decimal Total => _currencyService.DcimalRound((Quantity * ProductPrice));

        public string AdditionalDetails { get; set; }

        public string TotalString => _currencyService.FormatByUserCurrency((Total));

        public IEnumerable<ProductVariationOption> VariationOptions { get; set; } = new List<ProductVariationOption>();

        public IList<ProductFeesVM> ProductAdditionalFees { get; set; } = new List<ProductFeesVM>();

        public IList<ItemDetailAttribute> Attributes { get; set; } = new List<ItemDetailAttribute>();

        public static IEnumerable<ProductVariationOption> GetVariationOption(Product variation, long CultureId)
        {
            if (variation == null)
            {
                return new List<ProductVariationOption>();
            }
            //  return new List<ProductVariationOption>();

            return variation.OptionCombinations.Select(x => new ProductVariationOption
            {
                OptionName = x.Option.optionDetails.Where(x => CultureId == 0 || x.CultureId == CultureId).Select(x => x.Name).FirstOrDefault() ?? "",
                Value = x.Value.productOptionValueDetails == null ? "" : x.Value.productOptionValueDetails.Where(x => CultureId == 0 || x.CultureId == CultureId).Select(x => x.Value).FirstOrDefault() ?? ""
            });
        }

        public static IList<ProductFeesVM> GetAdditionalFees(Product Fees)
        {
            if (Fees == null)
            {
                return new List<ProductFeesVM>();
            }

            return Fees.ProductAdditionalFees.Where(x => x.Status != EStatus.Deleted).Select(x => new ProductFeesVM
            {
                Name = x.Name,
                FeesValue = x.FeeValue,
                FeesType = x.FeeType,
                ProductId = x.ProductId
            }).ToList();
        }
        public decimal? TaxPercent { get; set; }



    }

}
