using EskaCMS.Infrastructure.Pagination;
using EskaCMS.DynamicForms.ViewModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.DynamicForms.Services.IServices
{
    public interface IFormItemServiceNew
    {
        Task<PaginationResult<FormItemSummaryVM>> GetAllFormItemsWithClassificationPagination(long siteId, string? SearchText, int? page, int? pageSize);
        Task<PaginationResult<FormItemsClassificationVM>> GetClassificationsByFormItemIdPagintaion(long FormItemId, long siteId, string? SearchText, int? page, int? pageSize);
        Task<PaginationResult<FormFieldMainInfoVM>> GetFormFieldByFormItemIdPagintaion(long cultureId, long FormItemId, long siteId, int? page, int? pageSize);
    }
}
