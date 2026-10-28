using System;
using EskaCMS.Core.Areas.Core.ViewModels.Shared;
using EskaCMS.Entities.Entity;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Discount;
using System.Collections.Generic;
namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.PricingProductArea.Interfaces
{
    public interface IProductPricingService
    {
       
      

      


        CalculatedItemPrice CalculatedProductPrice(ProductThumbnail productThumbnail , string countryId  , decimal currencyRate , string currency, bool IsLocalCountry);

        CalculatedItemPrice CalculatedProductPrice(Product product  , string countryId , decimal currencyRate , string currency,bool IsForignPrice);

        CalculatedItemPrice CalculatedProductPrice(decimal? price,  decimal? specialPrice, DateTimeOffset? specialPriceStart, DateTimeOffset? specialPriceEnd, decimal? taxRate , decimal currencyRate, string currency, List<DiscountProduct> discountProducts = null);
    
    }
}
