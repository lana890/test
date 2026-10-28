using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCommerce.DomesticShipment.TopDelivery.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.DomesticShipment.TopDelivery.Services.Interfaces
{
    public interface ITopDeliveryServices
    {
        Task<DeliveryStatusViewModel> GetOrderHistory(long Id);
        Task<bool> CreateOrder(Order order,long?ShipmentId=null);
        Task<bool> RepushShipmentAsync(long shipmentId);
        Task<(string deliveryPrice, string deliveryTime)> EstimateDeliveryPriceAsync(Cart cart,Address address);
        Task CancelOrder(string trackingNumber,long OrderId,long shipmentId);
    }
}
