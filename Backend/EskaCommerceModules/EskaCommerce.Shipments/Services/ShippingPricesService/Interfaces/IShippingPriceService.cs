using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCommerce.ShippingPrices.Services;
using EskaCommerce.Shippments.ShippingPrices.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Shipments.Services.ShippingPricesService.Interfaces
{
    public interface IShippingPriceService
    {
        Task<IList<ShippingPrice>> GetApplicableShippingPrices(GetShippingPriceRequest request);
        Task<IList<ShippingPrice>> GetApplicableShippingPrices(GetShippingPriceRequest request, Cart cart);
    }
}
