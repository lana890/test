using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EskaCMS.Core.Areas.Core.ViewModels.Shared;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Entities.Entity.Catalog;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Entity.Tax;
using System.Threading.Tasks;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Enum;
using EskaCMS.Entities.Entity.Discount;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.PricingProductArea.Interfaces;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.PricingProductArea
{
    public class ProductPricingService : IProductPricingService
    {
        private readonly ICurrencyService _currencyService;
        private readonly IWorkContext workContext;
        private readonly IUserSiteService _userSiteService;


        public ProductPricingService(
            ICurrencyService currencyService,
            IWorkContext workContext,

            IUserSiteService userSiteService)
        {
            _currencyService = currencyService;
            this.workContext = workContext;
            _userSiteService = userSiteService;



        }


        public Task<decimal> GetTaxRate(TaxClass taxClass, string countryId)
        {
            return Task.FromResult(GetTaxRateValue(taxClass, countryId));
        }

        private static decimal GetTaxRateValue(TaxClass taxClass, string countryId)
        {

            if (taxClass.TaxRate == null || !taxClass.TaxRate.Any())
                return 0;
            var taxRate = taxClass.TaxRate.Where(x => x.CountryId == countryId).FirstOrDefault();

            if (taxRate != null)
                return taxRate.Rate;

            return 0;
        }

        public CalculatedItemPrice CalculatedProductPrice(ProductThumbnail productThumbnail, string countryId, decimal currencyRate, string currency, bool IsLocalCountry)
        {
            decimal taxRate = 0;

            if (productThumbnail.TaxClass != null)
            {
                taxRate = GetTaxRateValue(productThumbnail.TaxClass, countryId);

            }
            return CalculatedProductPrice(IsLocalCountry ? productThumbnail.Price : productThumbnail.ForeignPrice.HasValue? productThumbnail.ForeignPrice.Value: 0, IsLocalCountry ? productThumbnail.SpecialPrice : productThumbnail.SpecialForignPrice ?? null, productThumbnail.SpecialPriceStart, productThumbnail.SpecialPriceEnd, taxRate == 0 ? null : taxRate, currencyRate, currency);

        }

        public CalculatedItemPrice CalculatedProductPrice(Product product, string countryId, decimal currencyRate, string currency, bool IsLocalCountry)
        {
            try
            {

                decimal taxRate = 0;
                if (product.TaxClass != null)
                {
                    taxRate = GetTaxRateValue(product.TaxClass, countryId);

                }
                var applicablePrice = IsLocalCountry? product.PriceType==EPriceType.WarehouseBase
                                                             ?product.ProductPriceWarehouses?.Where(x=>x.WarehouseId==workContext.GetCurrentWarehouseIdValue()).Select(x=>x.Price).FirstOrDefault() ?? 0
                                                             :product.Price
                                                   : product.ForeignPrice ?? 0;
                var applicableSpecialPrice = IsLocalCountry ? product.SpecialPrice : product.SpecialForignPrice ?? 0;

                decimal? applicableTaxRate = taxRate == 0 ? null : taxRate;

                return CalculatedProductPrice(
                    applicablePrice,
                    applicableSpecialPrice,
                    product.SpecialPriceStart,
                    product.SpecialPriceEnd,
                    applicableTaxRate,
                    currencyRate,
                    currency,
                    product.DiscountProducts
                );
            }
            catch (Exception)
            {

                throw;
            }
        }

        //public CalculatedItemPrice CalculatedProductPrice(decimal? price, decimal? specialPrice, DateTimeOffset? specialPriceStart, DateTimeOffset? specialPriceEnd, decimal? taxRate, decimal currencyRate, string currency, List<DiscountProduct>? discountProducts = null)
        //{
        //    var UserId = workContext.GetCurrentUserIdValue();

        //    price = currencyRate * price;
        //    specialPrice = specialPrice.HasValue ? specialPrice * currencyRate : specialPrice;
        //    decimal DiscountValue = 0;
        //    var percentOfSaving = 0;
        //    var calculatedPrice = price;


        //    if (specialPrice.HasValue && specialPriceStart < DateTimeOffset.Now && DateTimeOffset.Now < specialPriceEnd)
        //    {
        //        calculatedPrice = specialPrice.Value;


        //    }
        //    if (discountProducts != null && discountProducts.Count() > 0)
        //    {

        //        foreach (var discountproduct in discountProducts)
        //        {
        //            if (discountproduct.Discount.IsCustomerGroup && discountproduct.Discount.CustomerGroups.Any())
        //            {
        //                var Users = discountproduct.Discount.CustomerGroups.SelectMany(x => x.CustomerGroup.Users).Select(x => x.UserId).ToList();
        //                if (Users.Any(x => x == UserId))
        //                {
        //                    DiscountValue += CalculateDiscountType(calculatedPrice ?? 0, discountproduct.Discount);
        //                }
        //            }
        //            else
        //            {
        //                DiscountValue += CalculateDiscountType(calculatedPrice ?? 0, discountproduct.Discount);

        //            }
        //        }
        //    }

        //    var Calculated = new CalculatedItemPrice(_currencyService)
        //    {
        //        Price = Math.Round((calculatedPrice ?? 0) - DiscountValue, 2),
        //        PriceOriginalString = Math.Round(price ?? 0, 2).ToString("0.00"),
        //        SpecialPrice = Math.Round(specialPrice ?? 0, 2),
        //        PercentOfSaving = percentOfSaving,
        //        PriceCurrency = currency,
        //    };



        //    if (taxRate.HasValue)
        //    {
        //        var specialprice = specialPrice.HasValue ? specialPrice.Value : 0;
        //        Calculated.Tax = (taxRate.Value / 100).ToString("0.00%");
        //        Calculated.SpecialPriceWithTax = _currencyService.DcimalRound((specialprice * (taxRate.Value / 100) + specialprice)).ToString("0.00");
        //        Calculated.PriceWithTax = _currencyService.DcimalRound((calculatedPrice ?? 0 * (taxRate.Value / 100) + calculatedPrice ?? 0)).ToString("0.00");
        //        return Calculated;
        //    }

        //    return Calculated;
        //}

        public CalculatedItemPrice CalculatedProductPrice(decimal? price, decimal? specialPrice, DateTimeOffset? specialPriceStart, DateTimeOffset? specialPriceEnd, decimal? taxRate, decimal currencyRate, string currency, List<DiscountProduct> discountProducts = null)
        {
            var UserId = workContext.GetCurrentUserIdValue();

            price = currencyRate * price;
            specialPrice = specialPrice.HasValue ? specialPrice * currencyRate : specialPrice;
            decimal DiscountValue = 0;
            var calculatedPrice = price;

            if (specialPrice.HasValue && specialPriceStart < DateTimeOffset.Now && DateTimeOffset.Now < specialPriceEnd)
            {
                calculatedPrice = specialPrice.Value;
            }

            if (discountProducts != null && discountProducts.Count() > 0)
            {
                foreach (var discountproduct in discountProducts)
                {
                    if (discountproduct.Discount.StartDate.HasValue && discountproduct.Discount.StartDate > DateTimeOffset.UtcNow)
                        continue;
                    if (discountproduct.Discount.ExpiryDate.HasValue && discountproduct.Discount.ExpiryDate < DateTimeOffset.UtcNow)
                        continue;

              
                        if (discountproduct.Discount.IsCustomerGroup && discountproduct.Discount.CustomerGroups.Any())
                        {
                            var Users = discountproduct.Discount.CustomerGroups.SelectMany(x => x.CustomerGroup.Users).Select(x => x.UserId).ToList();
                            if (Users.Any(x => x == UserId))
                            {
                                DiscountValue += CalculateDiscountType(calculatedPrice ?? 0, discountproduct.Discount);
                            }
                        }
                        else
                        {
                            DiscountValue += CalculateDiscountType(calculatedPrice ?? 0, discountproduct.Discount);
                        }
                    
                  
                }
            }

            var Calculated = new CalculatedItemPrice(_currencyService)
            {
                Price = Math.Round((calculatedPrice ?? 0) - DiscountValue, 2),
                PriceOriginalString = Math.Round(price ?? 0, 2).ToString("0.00"),
                SpecialPrice = Math.Round(specialPrice ?? 0, 2),
                PriceCurrency = currency,
            };

            var savings = (price ?? 0) - Calculated.Price;
            Calculated.PercentOfSaving = price.HasValue && price.Value > 0
                ? (int)Math.Round(savings / price.Value * 100, 0)
                : 0;

            if (taxRate.HasValue)
            {
                var specialprice = specialPrice.HasValue ? specialPrice.Value : 0;
                var basePrice = calculatedPrice ?? 0;

                Calculated.Tax = (taxRate.Value / 100).ToString("0.00%");

                Calculated.SpecialPriceWithTax = _currencyService
                    .DcimalRound(specialprice * (taxRate.Value / 100) + specialprice)
                    .ToString("0.00");

                Calculated.PriceWithTax = _currencyService
                    .DcimalRound(basePrice * (taxRate.Value / 100) + basePrice)
                    .ToString("0.00");

                return Calculated;
            }

            return Calculated;
        }

        private decimal CalculateDiscountType(decimal tiersAmount, Discount discountInfo)
        {
            switch (discountInfo.DiscountType)
            {
                case DiscountTypes.Percentage:
                    return tiersAmount * discountInfo.DiscountValue / 100;

                case DiscountTypes.FixedAmount:
                    return Convert.ToDecimal(discountInfo.DiscountValue);

                case DiscountTypes.Tiers:
                    if (discountInfo.DiscountTiers != null)
                    {
                        foreach (var tier in discountInfo.DiscountTiers)
                        {
                            if (tiersAmount >= tier.FromValue && tiersAmount <= tier.ToValue)
                            {
                                if (tier.DiscountTierType == DiscountTiersTypes.FixedAmount)
                                {
                                    return tier.DiscountValue;
                                }
                                else if (tier.DiscountTierType == DiscountTiersTypes.Percentage)
                                {
                                    return tiersAmount * tier.DiscountValue / 100;
                                }
                            }
                        }
                    }
                    break;
            }

            return 0;
        }
    }
}
