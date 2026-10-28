using EskaCMS.Entities.Entity.Orders;
using EskaCommerce.Orders.Areas.Orders.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Orders.Services
{
    public interface IOrderAdditionalFeesServices
    {
        Task<bool> AddOrderAdditionalFees(List<OrderAdditionalFees> orderAdditionalFees, long OrderId);
        Task<bool> UpdateFeesToRefaund(UpdateOrderAdditionalFeesVM update);
    }
}
