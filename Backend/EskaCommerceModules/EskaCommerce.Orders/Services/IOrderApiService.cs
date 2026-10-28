using EskaCommerce.Orders.Areas.Orders.ViewModels;
using System.Threading.Tasks;

namespace EskaCommerce.Orders.Services
{
    public interface IOrderApiService
    {
        GridOutputVM<OrderListOutputVM> GetFilteredUserOrders(long? userId, SearchTableParam<OrderListSearchVM> param);

        Task updateAddresses(long id, UpdateAddressVM addressesVM, long CurrentUser);
        Task updateAdditionalAddresses(long id, UpdateAddressVM addressesVM, long CurrentUser);
                
    }
}
