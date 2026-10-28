using EEskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.UserDashboard;
using EskaCMS.Entities.Enum;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices
{
    public static class QuerableExtensions
    {

        public static IQueryable<Product> FilterPrice(this IQueryable<Product>  products, ProductSearchViewModel productSearch,bool IsLocalCountry,long WarehouseId)
        {

            if (productSearch.PriceRange != null)
            {
                if (productSearch.PriceRange.ToPrice != null)
                {
                    if (IsLocalCountry)
                    {
                        products = products.Where(x =>
                            x.PriceType == EPriceType.WarehouseBase &&
                             x.ProductPriceWarehouses.Any(pw => pw.WarehouseId == WarehouseId &&
                                                                 pw.Price <= productSearch.PriceRange.ToPrice.Value)
                            ||
                            x.PriceType == EPriceType.ProductBase &&
                             x.Price <= productSearch.PriceRange.ToPrice.Value
                        );
                    }
                    else
                    {
                        products = products.Where(x => x.ForeignPrice == null ||
                                                       x.ForeignPrice.Value <= productSearch.PriceRange.ToPrice.Value);
                    }
                }

                if (productSearch.PriceRange.FromPrice != null)
                {
                    if (IsLocalCountry)
                    {
                        products = products.Where(x =>
                            x.PriceType == EPriceType.WarehouseBase &&
                             x.ProductPriceWarehouses.Any(pw => pw.WarehouseId == WarehouseId &&
                                                                 pw.Price >= productSearch.PriceRange.FromPrice.Value)
                            ||
                            x.PriceType == EPriceType.ProductBase &&
                             x.Price >= productSearch.PriceRange.FromPrice.Value
                        );
                    }
                    else
                    {
                        products = products.Where(x => x.ForeignPrice == null ||
                                                       x.ForeignPrice.Value >= productSearch.PriceRange.FromPrice.Value);
                    }
                }
            }


            return products;

        }


      
    }
}
