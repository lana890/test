using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.ShippingTableRate.Areas.ShippingTableRate.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.ShippingTableRate.Services.Interface
{
    public interface IShippingTableRateServices
    {
        Task<bool> ChangeStatus(long id, GeneralEnums.EStatus status);
        Task<bool> CreateRate(List<RequestRateAndDistination> model);
        Task<bool> EditRate(List<RequestRateAndDistination> model);
        Task<PaginationResult<PriceAndDestinationForm>> GetAll(SearchPriceAndDistinationViewModel model);
        Task<PriceAndDestinationForm> GetById(long id);
        Task<object> UpsertRate(List<RequestRateAndDistination> model);
    }
}