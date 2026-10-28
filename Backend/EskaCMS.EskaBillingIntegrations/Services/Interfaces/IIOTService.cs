

using AutoMapper;
using EskaCMS.EskaBilling.Models;
using EskaCMS.EskaBilling.Models.response;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Helpers.DinToPdf;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Integrations.EskaBilling.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static EskaCMS.EskaBilling.Enums.IOTEnums;

namespace EskaCMS.EskaBilling.Services
{
    public interface IIOTService
    {
        Task<bool> AddSIM(CustomerSimsVM CustomerSimslst, long CustomerId, long ProductId);
        Task<bool> AddBulkSIMS(IFormFile CustomerSimsSheet, long CustomerId, long ProductId);


        Task<PaginationResult<CustomerSimsResponseViewModel>> GetCustomerSIMs(SmartTableParam param, long customerId);
        Task<SimsViewModel> GetSIM(long SIMId);


        Task<bool> UpdateSIM(CustomerSimsUpdateViewModel customerSimslst);
        Task<bool> DeleteSIM(long SIMId);

        Task<List<CustomerAccountsVM>> GetCustomerAccounts(long? CustomerId);
        Task<PaginationResult<SimsHistoryVM>> GetSIMHistory(SmartTableParam param);


        Task<SimInvoiceVM> GetSimInvoice(GetSimInvoiceVM simInvoice);
        Task<InvoiceDetailsVM> GetInvoiceDetails(long invoiceId);
        Task<PaginationResult<SimInvoiceVM>> GetCustomerInvoice(SmartTableParam param, long customerId);
        Task<Infrastructure.Helpers.DinToPdf.PdfFile> ExportCustomerInvoice(SmartTableParam param, long customerId);
        Task<Infrastructure.Helpers.DinToPdf.PdfFile> ExportSimInvoice(GetSimInvoiceVM simInvoice);

        Task<List<long>> GetCustomerSIMsId(SmartTableParam param, long customerId);
        Task<bool> UpdateBulkSims(UpdateBulkSimsVM model);
        Task<bool> DeleteRequstedPlan(long Id);
        Task<PaginationResult<RequestChangePlanVM>> GetShechduledChangePlans(SmartTableParam param, long customerId);

        Task<bool> ChangeInvoiceStatus(ChangeInvoiceStatusVM model, long userId);

        Task<List<long>> GetInvoicesId(SmartTableParam param, long customerId);

    }
}

