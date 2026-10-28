using EskaCMS.Entities.Entity.Orders;
using EskaCommerce.Orders.Areas.Orders.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Orders.Services.OrderAddressServices.Interfaces
{
    public interface IUserAddressService
    {
        Task<bool> Add(OrderAddressViewModel model);
        Task<bool> Update(OrderAddressViewModel model);

        Task<List<OrderAddresssResponseViewModel>> GetAddress(long? userId, string countryId);


        Task<bool> Delete(long Id);


    }
}
