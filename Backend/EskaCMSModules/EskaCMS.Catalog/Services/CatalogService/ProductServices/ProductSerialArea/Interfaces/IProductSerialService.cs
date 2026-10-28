using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Pages.Models;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Entities.Entity
;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductSerialArea.Interfaces
{
    public interface IProductSerialService
    {
        Task<SmartTableResult<ProductSerialViewModel>> GetAllProductSerials(SmartTableParam Param);
        Task<bool> Create(List<ProductSerialViewModel> product, long productId);
        Task<List<ProductSerialViewModel>> ImportFile(IFormFile file);
        Task<bool> ChangeProductSerialStatus(long serialId, EProductSerialStatus NewStatus);
        Task<List<ProductSerialHistoryViewModel>> GetProductSerialHistory(long productSerialId);

        Task<List<SerialsExportVm>> GetSerialsToExport(SmartTableParam Param);

        Task<bool> AddSerialsFromRanges(long ProductId, List<Range<long>> Ranges);
        Task<List<Range<long>>> GetAllSerialsAsRanges(long ProductId);
        Task<bool> DeleteRange(long ProductId, List<Range<long>> Ranges);


        Task<List<SerialsExportWithOwnersVM>> GetSerialsWithOwners(long ProductId);
        Task<bool> ReserveSerials(long productSerialId, long orderId);
        Task<bool> RestoreSerials(long productSerialId);
    }
}
