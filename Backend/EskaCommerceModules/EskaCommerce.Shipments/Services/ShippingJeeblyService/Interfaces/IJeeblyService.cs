using EskaCMS.Entities.Entity;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Orders;

namespace EskaCommerce.Shipments.Services.ShippingJeeblyService.Interfaces
{
    public interface IJeeblyService
    {
        Task CreateShipmentOrder(Order request);
    }
}
