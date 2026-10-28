using EskaCMS.Entities.Entity.Orders;
using EskaCommerce.DomesticShipment.SimsCreation.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.DomesticShipment.SimsCreation.Services.Interfaces
{
    public interface ISimsCreationServices
    {
        Task<OrderResponseViewModel> CreateOrder(long orderId);
        Task<OrderUserViewModel> GetOrderById(string U_Id);
        Task<List<ServiceProviderViewModel>> GetAllServiceProvider();
        Task<DeleteOrderViewModel> DeleteOrder(string id);
    }
}
