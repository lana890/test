using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Shipping;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCommerce.ShippingPrices.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Shipments.Services.ShippingPricesService.Interfaces
{
    public interface IShippingPriceServiceProvider
    {
        Task<GetShippingPriceResponse> GetShippingPrices(GetShippingPriceRequest request, ShippingProvider provider);
        Task<GetShippingPriceResponse> GetShippingPrices(GetShippingPriceRequest request, ShippingProvider provider, Cart cart);

    }
}
