using Microsoft.EntityFrameworkCore;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Core.Extensions;
using EskaCMS.Integrations.EskaBilling.Models;
using EskaCMS.Core.Data;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.EskaBilling.Enums;
using static EskaCMS.EskaBilling.Enums.IOTEnums;
using Microsoft.AspNetCore.Http;
using EskaCMS.Infrastructure.Helpers;
using AutoMapper;
using EskaCMS.EskaBilling.Models.response;
using AutoMapper.QueryableExtensions;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Core.Entities;
using EskaCMS.Infrastructure.Helpers.HttpHelper;
using System.Globalization;
using EskaCMS.EskaBilling.Models;

using EskaCMS.Core.Services;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using System.Text;
using EskaCMS.Infrastructure.Helpers.DinToPdf;
using EskaCMS.Entities.Entity.Core;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.DataAccess.Data;
using EskaCMS.Entities.Entity;

namespace EskaCMS.EskaBilling.Services
{
    public class IOTService : IIOTService
    { 

        private IConfiguration _configuration;
        private readonly IWorkContext _workContext;

        private readonly IRepository<CustomerSims> _customerSimsRepository;
        private readonly IRepository<CustomerSimsHistory> _customerSimsHistoryRepository;
        private readonly IRepository<CustomerInvoices> _customerInvoicesRepository;
        private readonly IRepository<SIMUsage> _simsUsageRepository;
        private readonly IRepository<ChangePlanRequest> _changePlanRequestRepository;
        private readonly IRepository<SitesCultures> _sitesCulturesRepository;
        private readonly IRepositoryWithTypedId<SiteSettings, string> _siteSettingRepository;
        private readonly IRepositoryWithTypedId<EmailTemplates, string> _emailTemplates;

        private readonly ISIMsActionsService _SIMsActionsService;
        private readonly EskaDCMSDbContext _context;
        private readonly IMapper mapper;
        private readonly IpdfConverter _ipdfConverter;
        private readonly ICurrencyService _currencyService;

        private const string CultureId = "en-US";


        public IOTService(IWorkContext workContext, IRepository<SIMUsage> simsUsageRepository
                        , IRepository<ChangePlanRequest> changePlanRequestRepository
                        , IRepository<CustomerInvoices> customerInvoicesRepository
                        , IRepository<CustomerSims> customerSimsRepository
                        , IRepository<SitesCultures> sitesCulturesRepository
                        , EskaDCMSDbContext context
                        , IMapper mapper
            , IpdfConverter ipdfConverter
                        , ICurrencyService currencyService
                        , IConfiguration configuration
                        , IRepositoryWithTypedId<SiteSettings, string> siteSettingRepository
                        , IRepositoryWithTypedId<EmailTemplates, string> emailTemplates
                        , IRepository<CustomerSimsHistory> customerSimsHistoryRepository
                        , ISIMsActionsService sIMsActionsService)
        {
            _workContext = workContext;
            _customerSimsRepository = customerSimsRepository;
            _context = context;
            this.mapper = mapper;
            _customerSimsHistoryRepository = customerSimsHistoryRepository;
            _customerInvoicesRepository = customerInvoicesRepository;
            _simsUsageRepository = simsUsageRepository;
            _changePlanRequestRepository = changePlanRequestRepository;
            _sitesCulturesRepository = sitesCulturesRepository;
            _SIMsActionsService = sIMsActionsService;
            _currencyService = currencyService;
            _siteSettingRepository = siteSettingRepository;
            _configuration = configuration;
            _emailTemplates = emailTemplates;
            _ipdfConverter = ipdfConverter;
        }



        #region Add
        public async Task<bool> AddSIM(CustomerSimsVM SIM, long CustomerId, long ProductId)
        {
            try
            {

                try
                {
                    long UserId = await _workContext.GetCurrentUserId();
                    long SiteId = await _workContext.GetCurrentSiteIdAsync();
                    CustomerSims objCustomerSims = new CustomerSims();


                    objCustomerSims.CustomerId = CustomerId;
                    objCustomerSims.AccountId = SIM.AccountId;
                    objCustomerSims.OrderId = SIM.OrderId;

                    objCustomerSims.MSISDN = SIM.MSISDN;
                    objCustomerSims.Status = EIOTSIMStatus.WaitingForActivation;
                    objCustomerSims.IdentifierType = GetESIMsIdentifierType(SIM.IMSI, SIM.ICCID);
                    objCustomerSims.CreatedById = UserId;
                    objCustomerSims.CreationDate = DateTimeOffset.Now;
                    objCustomerSims.SiteId = SiteId;
                    objCustomerSims.APNType = SIM.APNType;
                    objCustomerSims.ICCID = SIM.ICCID;
                    objCustomerSims.SIMName = SIM.SIMName;
                    objCustomerSims.IMSI = SIM.IMSI;

                    objCustomerSims.ProductId = ProductId;
                    objCustomerSims.SIMTag = SIM.SIMTag;
                    objCustomerSims.City = SIM.City;





                    ValidateSIM_ICCID_And_IMSI(objCustomerSims);
                    _customerSimsRepository.Add(objCustomerSims);
                    await _customerSimsRepository.SaveChangesAsync();

                    CustomerSimsHistory objCustomerSimsHistory = new CustomerSimsHistory(objCustomerSims, null, "Add New.", UserId);
                    _customerSimsHistoryRepository.Add(objCustomerSimsHistory);
                    await _customerSimsHistoryRepository.SaveChangesAsync();

                    return true;
                }
                catch (Exception ex)
                {
                    ex = ExceptionHelper.ThrowDeepException(ex);
                    if (ex.Message.Contains("IX_Integrations_CustomerSims_ICCID"))
                    {
                        throw new Exception("Invalid Or Duplicated ICCID");
                    }
                    else if (ex.Message.Contains("IX_Integrations_CustomerSims_IMSI"))
                    {
                        throw new Exception("Invalid Or Duplicated IMSI");
                    }
                    else if (ex.Message.Contains("IX_Integrations_CustomerSims_MSISDN"))
                    {
                        throw new Exception("Invalid Or Duplicated MSISDN");
                    }
                    else
                    {
                        throw ex;

                    }

                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<bool> AddBulkSIMS(IFormFile CustomerSimsSheet, long CustomerId, long ProductId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
            await using var transactions = await _context.Database.BeginTransactionAsync();
            try
            {

                    List<CustomerSimsVM> CustomerSimslst = await ExcelHelper.ImportExcel<CustomerSimsVM>(CustomerSimsSheet);



                    var ListCustomerSims = new List<CustomerSims>();

                    foreach (var SIM in CustomerSimslst)
                    {
                        await AddSIM(SIM, CustomerId, ProductId);
                    }
                    await transactions.CommitAsync();

                    return true;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });

        }


        #endregion

        #region GET
        public async Task<PaginationResult<CustomerSimsResponseViewModel>> GetCustomerSIMs(SmartTableParam param, long customerId)
        {
            try
            {
                long SiteId = await _workContext.GetCurrentSiteIdAsync();

                var query = _customerSimsRepository.Query()
                    .Where(x => x.SiteId == SiteId &&
                                x.CustomerId == customerId &&
                                !x.IsDeleted)
                    .Include(x => x.Product)
                    .Include(x => x.Customer)
                    .AsNoTrackingWithIdentityResolution();

                if (param.Search.PredicateObject != null)
                {
                    dynamic search = param.Search.PredicateObject;
                    if (search.ICCID != null)
                    {
                        long ICCID = search.ICCID;
                        query = query.Where(x => x.ICCID == ICCID);
                    }


                    if (search.Status != null)
                    {
                        var status = (EIOTSIMStatus)search.Status;
                        query = query.Where(x => x.Status == status);
                    }


                    if (search.ProductId != null)
                    {
                        long ProductId = Convert.ToInt64(search.ProductId);
                        query = query.Where(x => x.ProductId == ProductId);
                    }


                    if (search.AccountId != null)
                    {

                        string AccountId = Convert.ToString(search.AccountId);
                        query = query.Where(x => x.AccountId.ToString().Contains(AccountId));
                    }
                    if (search.OrderId != null)
                    {
                        string OrderId = Convert.ToString(search.OrderId);
                        query = query.Where(x => x.OrderId.ToString().Contains(OrderId));
                    }
                    if (search.ProductName != null)
                    {
                        string ProductName = search.ProductName;
                        ProductName = ProductName.ToLower();

                        query = query.Where(x => x.Product.ProductDetails.Any(x =>x.Name.ToLower().Contains(ProductName)));
                    }
                    if (search.City != null)
                    {
                        string City = search.City;
                        City = City.ToLower();

                        query = query.Where(x => x.City.ToLower().Contains(City));
                    }
                    if (search.Tag != null)
                    {
                        string Tag = search.Tag;
                        Tag = Tag.ToLower();
                        query = query.Where(x => x.SIMTag.ToLower().Contains(Tag));
                    }

                    if (search.SIMName != null)
                    {
                        string SIMName = search.SIMName;
                        SIMName = SIMName.ToLower();
                        query = query.Where(x => x.SIMName.ToLower().Contains(SIMName));
                    }
                }

                var result = await query.ProjectTo<CustomerSimsResponseViewModel>(mapper.ConfigurationProvider)
                    .OrderByDescending(x => x.CreationDate)
                    .PaginateAsync(param.Pagination.Number, param.Pagination.Start);

                return result;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<List<long>> GetCustomerSIMsId(SmartTableParam param, long customerId)
        {
            try
            {
                long SiteId = await _workContext.GetCurrentSiteIdAsync();

                var query = _customerSimsRepository.Query()
                    .Where(x => x.SiteId == SiteId &&
                                x.CustomerId == customerId &&
                                !x.IsDeleted)
                    .Include(x => x.Product)
                    .Include(x => x.Customer)
                    .AsNoTrackingWithIdentityResolution();

                if (param.Search.PredicateObject != null)
                {
                    dynamic search = param.Search.PredicateObject;
                    if (search.ICCID != null)
                    {
                        long ICCID = search.ICCID;
                        query = query.Where(x => x.ICCID == ICCID);
                    }


                    if (search.Status != null)
                    {
                        var status = (EIOTSIMStatus)search.Status;
                        query = query.Where(x => x.Status == status);
                    }


                    if (search.ProductId != null)
                    {
                        long ProductId = Convert.ToInt64(search.ProductId);
                        query = query.Where(x => x.ProductId == ProductId);
                    }


                    if (search.AccountId != null)
                    {
                        long AccountId = Convert.ToInt64(search.AccountId);
                        query = query.Where(x => x.AccountId == AccountId);
                    }
                    if (search.ProductName != null)
                    {
                        string ProductName = search.ProductName;
                        ProductName = ProductName.ToLower();

                        query = query.Where(x => x.Product.ProductDetails.Any(x => x.Name.ToLower().Contains(ProductName)));
                    }
                    if (search.City != null)
                    {
                        string City = search.City;
                        City = City.ToLower();

                        query = query.Where(x => x.City.ToLower().Contains(City));
                    }
                    if (search.Tag != null)
                    {
                        string Tag = search.Tag;
                        Tag = Tag.ToLower();
                        query = query.Where(x => x.SIMTag.ToLower().Contains(Tag));
                    }

                    if (search.SIMName != null)
                    {
                        string SIMName = search.SIMName;
                        SIMName = SIMName.ToLower();
                        query = query.Where(x => x.SIMName.ToLower().Contains(SIMName));
                    }
                }

                var result = query.Select(x => x.Id).ToList();

                return result;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<SimInvoiceVM> GetSimInvoice(GetSimInvoiceVM simInvoice)
        {
            try
            {

                long cultureId = 0;
                var plattform = _workContext.GetCurrentPlatform();
                var SiteId = _workContext.GetCurrentSiteId();
                if (plattform == EPlatformSource.Admin)
                {
                    cultureId = _sitesCulturesRepository.Query().Where(x => x.CultureId == CultureId && x.SiteId == SiteId).FirstOrDefault().Id;
                }
                else
                {
                    cultureId = _workContext.GetCurrentCultureId();
                }

                var query = _customerInvoicesRepository.Query()
                    .Where(x => x.CustomerSimsId == simInvoice.Id &&
                    x.StartDate.Month == simInvoice.Date.Month &&
                    x.StartDate.Year == simInvoice.Date.Year)
                    .Include(x => x.CustomerSims)
                    .Include(x => x.Customer)
                    .Include(x => x.Product)
                    .ThenInclude(x => x.ProductDetails)
                    
                    .AsNoTrackingWithIdentityResolution()
                    .FirstOrDefault();




                var previousAmount = _customerInvoicesRepository.Query()
                    .Where(x => x.CustomerSimsId == simInvoice.Id &&
                    x.StartDate.Month < simInvoice.Date.Month &&
                    x.StartDate.Year == simInvoice.Date.Year &&
                    x.PaymentStatus == EInvoiceStatus.UnPaid)
                    .Sum(x => x.TotalAmount);

                var result = mapper.Map<SimInvoiceVM>(query);


                if (result is not null)
                {

                    result.ServiceName =  query.Product.ProductDetails.Where(x => x.CultureId == cultureId).FirstOrDefault()?.Name;
                    result.Description = query.Product.ProductDetails.Where(x => x.CultureId == cultureId).FirstOrDefault()?.Name;
                    result.PreviousAmount = previousAmount + " SAR";

                }


                return result;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
        public async Task<bool> ChangeInvoiceStatus(ChangeInvoiceStatusVM model, long userId)
        {
            var invoiceToUpdate = _customerInvoicesRepository.Query()
                                                     .Where(x => model.InovicesId.Contains(x.Id))
                                                     .ToList();
            if (!invoiceToUpdate.Any())
            {
                throw new Exception($"Can't find  invoices ");
            }
            foreach (var invoice in invoiceToUpdate)
            {
                invoice.PaymentStatus = model.Status;
                invoice.ModifiedById = userId;
                invoice.ModificationDate = DateTimeOffset.Now;
            }

            await _customerInvoicesRepository.SaveChangesAsync();

            return true;

        }
        public async Task<PaginationResult<SimInvoiceVM>> GetCustomerInvoice(SmartTableParam param, long customerId)
        {
            try
            {


                var query = _customerInvoicesRepository.Query()
                    .Where(x => x.CustomerId == customerId)
                    .Include(x => x.Customer)
                    .Include(x => x.CustomerSims)
                    .AsNoTrackingWithIdentityResolution();





                if (param.Search.PredicateObject != null)
                {
                    dynamic search = param.Search.PredicateObject;
                    if (search.Date != null)
                    {
                        DateTimeOffset Date = search.Date;
                        query = query.Where(x => x.StartDate.Month == Date.Month && x.StartDate.Year == Date.Year);
                    }


                    if (search.AccountId != null)
                    {
                        long AccountId = Convert.ToInt64(search.AccountId);
                        query = query.Where(x => x.AccountId == AccountId);
                    }


                    if (search.Status != null)
                    {
                        var status = (EInvoiceStatus)search.Status;
                        query = query.Where(x => x.PaymentStatus == status);
                    }

                    if (search.AccountId != null)
                    {
                        long AccountId = Convert.ToInt64(search.AccountId);
                        query = query.Where(x => x.AccountId == AccountId);
                    }


                    if (search.Tag != null)
                    {
                        string Tag = search.Tag;
                        Tag = Tag.ToLower();
                        query = query.Where(x => x.CustomerSims.SIMTag.ToLower().Contains(Tag));
                    }

                    if (search.SIMName != null)
                    {
                        string SIMName = search.SIMName;
                        SIMName = SIMName.ToLower();
                        query = query.Where(x => x.CustomerSims.SIMName.ToLower().Contains(SIMName));
                    }
                }

                var result = await query.ProjectTo<SimInvoiceVM>(mapper.ConfigurationProvider)

                    .PaginateAsync(param.Pagination.Number, param.Pagination.Start);




                return result;


            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<List<long>> GetInvoicesId(SmartTableParam param, long customerId)
        {
            try
            {


                var query = _customerInvoicesRepository.Query()
                    .Where(x => x.CustomerId == customerId)
                    .Include(x => x.Customer)
                    .Include(x => x.CustomerSims)
                    .AsNoTrackingWithIdentityResolution();





                if (param.Search.PredicateObject != null)
                {
                    dynamic search = param.Search.PredicateObject;
                    if (search.Date != null)
                    {
                        DateTimeOffset Date = search.Date;
                        query = query.Where(x => x.StartDate.Month == Date.Month && x.StartDate.Year == Date.Year);
                    }


                    if (search.AccountId != null)
                    {
                        long AccountId = Convert.ToInt64(search.AccountId);
                        query = query.Where(x => x.AccountId == AccountId);
                    }


                    if (search.Status != null)
                    {
                        var status = (EInvoiceStatus)search.Status;
                        query = query.Where(x => x.PaymentStatus == status);
                    }

                    if (search.AccountId != null)
                    {
                        long AccountId = Convert.ToInt64(search.AccountId);
                        query = query.Where(x => x.AccountId == AccountId);
                    }


                    if (search.Tag != null)
                    {
                        string Tag = search.Tag;
                        Tag = Tag.ToLower();
                        query = query.Where(x => x.CustomerSims.SIMTag.ToLower().Contains(Tag));
                    }

                    if (search.SIMName != null)
                    {
                        string SIMName = search.SIMName;
                        SIMName = SIMName.ToLower();
                        query = query.Where(x => x.CustomerSims.SIMName.ToLower().Contains(SIMName));
                    }
                }

                var result = query.Select(x => x.Id).ToList();




                return result;


            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<Infrastructure.Helpers.DinToPdf.PdfFile> ExportCustomerInvoice(SmartTableParam param, long customerId)
        {
            try
            {
                long cultureId = 0;
                var plattform = _workContext.GetCurrentPlatform();
                var SiteId = _workContext.GetCurrentSiteId();
                if (plattform == EPlatformSource.Admin)
                {
                    cultureId = _sitesCulturesRepository.Query().Where(x => x.CultureId == CultureId && x.SiteId == SiteId).FirstOrDefault().Id;
                }
                else
                {
                    cultureId = _workContext.GetCurrentCultureId();
                }

                var query = _customerInvoicesRepository.Query()
                    .Where(x => x.CustomerId == customerId && x.PaymentStatus == EInvoiceStatus.UnPaid)
                    .Include(x => x.CustomerSims)
                    .Include(x => x.Customer)
                    .Include(x => x.Product)
                    .ThenInclude(x => x.ProductDetails)
                    .AsNoTrackingWithIdentityResolution();


                var previousUnpaidInvoices = _customerInvoicesRepository.Query()
                                        .Where(x => x.CustomerId == customerId && x.PaymentStatus == EInvoiceStatus.UnPaid)
                                        .Include(x => x.CustomerSims)
                                        .AsNoTrackingWithIdentityResolution();

                if (param.Search.PredicateObject != null)
                {
                    dynamic search = param.Search.PredicateObject;
                    if (search.Date != null)
                    {
                        DateTimeOffset Date = search.Date;

                        query = query.Where(x => x.StartDate.Month == Date.Month && x.StartDate.Year == Date.Year);
                        previousUnpaidInvoices = previousUnpaidInvoices.Where(x => x.StartDate.Month < Date.Month && x.StartDate.Year == Date.Year);
                    }


                    if (search.AccountId != null)
                    {
                        long AccountId = Convert.ToInt64(search.AccountId);
                        query = query.Where(x => x.AccountId == AccountId);
                        previousUnpaidInvoices = previousUnpaidInvoices.Where(x => x.AccountId == AccountId);


                    }

                    if (search.Tag != null)
                    {
                        string Tag = search.Tag;
                        Tag = Tag.ToLower();
                        query = query.Where(x => x.CustomerSims.SIMTag.ToLower().Contains(Tag));
                        previousUnpaidInvoices = previousUnpaidInvoices.Where(x => x.CustomerSims.SIMTag.ToLower().Contains(Tag));

                    }

                    if (search.SIMName != null)
                    {
                        string SIMName = search.SIMName;
                        SIMName = SIMName.ToLower();
                        query = query.Where(x => x.CustomerSims.SIMName.ToLower().Contains(SIMName));
                        previousUnpaidInvoices = previousUnpaidInvoices.Where(x => x.CustomerSims.SIMName.ToLower().Contains(SIMName));
                    }
                }


                var allsimInvoice = query.ToList();

                if (!allsimInvoice.Any())
                {
                    return null;
                }

                var allPreviousInvoice = previousUnpaidInvoices.ToList();


                var summarySimInvoices = GetSummarySimInvoices(allsimInvoice,cultureId);



                var calculateInvoice = CalculateCustomerInvoice(allsimInvoice, allPreviousInvoice);


                var invoiceOfOneSim = summarySimInvoices.FirstOrDefault();

                var customerInvoice = new CustomerInvoicesVm
                {
                    AccountId = invoiceOfOneSim.AccountId,
                    CompanyVatId = invoiceOfOneSim.CompanyVatId,
                    CustomerVatId = invoiceOfOneSim.CustomerVatId,
                    CustomerAddress = invoiceOfOneSim.CustomerAddress,
                    PreviousUnPaidAmount = calculateInvoice.AllPreviousUnpaid + " SAR",
                    TotalAfterTaxAmount = calculateInvoice.TotalAfterTax + " SAR",
                    TotalTaxAmount = calculateInvoice.TotalTaxAmount + " SAR",
                    TotalSubAmount = calculateInvoice.TotalSubAmount + " SAR",
                    TotalDueAmount = calculateInvoice.AllDueAmount + " SAR",
                    CreationDate = invoiceOfOneSim.CreationDate,
                    CustomerName = invoiceOfOneSim.CustomerName,

                };


                var InoviceTemplate = await GetCustomerInvoiceTemplate(summarySimInvoices);


               // var Invoice = await _ipdfConverter.Convert(InoviceTemplate, customerInvoice, "Invoice");
                var Invoice = await _ipdfConverter.Convert(InoviceTemplate, new List<PdfData>(), "Invoice");

                return Invoice;







            }
            catch (Exception exc)
            {

                throw exc;
            }
        }



        public async Task<InvoiceDetailsVM> GetInvoiceDetails(long invoiceId)
        {
            try
            {


                var query = _customerInvoicesRepository.Query()
                    .Where(x => x.Id == invoiceId)
                    .AsNoTrackingWithIdentityResolution().FirstOrDefault();

                var usages = await _simsUsageRepository.Query()
                                                  .Where(x => x.CustomerSimsId == query.CustomerSimsId &&
                                                         x.CreationDate.Month == query.StartDate.Month &&
                                                        x.CreationDate.Year == query.StartDate.Year)
                                                    .OrderBy(x => x.CreationDate)
                                                    .ToListAsync();

                if (query is null || !usages.Any())
                {

                    return new InvoiceDetailsVM();
                }

                var activePeriods = new List<Period>();
                var terminatedPeriods = new List<Period>();
                var suspendedPeriods = new List<Period>();
                var waitingPeriods = new List<Period>();

                DateTimeOffset startDate = new DateTimeOffset(query.StartDate.Date, query.StartDate.Offset);

                foreach (var simUsage in usages)
                {


                    UpdateStatusPeriod(simUsage.StatusSnapshot, simUsage.CreationDate, startDate, activePeriods, terminatedPeriods, suspendedPeriods, waitingPeriods);

                    startDate = simUsage.CreationDate;
                }

                var InvoiceDetails = new InvoiceDetailsVM
                {
                    ActivePeriods = activePeriods,
                    SuspendedPeriods = suspendedPeriods,
                    TerminatedPeriods = terminatedPeriods,
                    WatingPeriods = waitingPeriods,
                };

                return InvoiceDetails;

            }
            catch (Exception exc)
            {

                throw exc;
            }
        }



        public async Task<Infrastructure.Helpers.DinToPdf.PdfFile> ExportSimInvoice(GetSimInvoiceVM model)
        {
            try
            {

                var siteId = _workContext.GetCurrentSiteId();
                var settings = _siteSettingRepository.Query().AsNoTracking()
                                                        .Where(x => x.SiteId == siteId && x.Id == "SimInvoiceTemplate")
                                                        .FirstOrDefault();
                var templateId = settings.Value;

                var template = _emailTemplates.Query().AsNoTracking()
                                               .Where(x => x.Id == templateId && x.Status == EStatus.Active)
                                               .FirstOrDefault();

                var simInvoice = await GetSimInvoice(model);

                if (simInvoice is null)
                     
                    return null;

               // var Invoice = await _ipdfConverter.Convert(template.EmailBody, simInvoice, "Invoice");
                var Invoice = await _ipdfConverter.Convert(template.EmailBody, new List<PdfData>(), "Invoice");

                return Invoice;





            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<List<CustomerAccountsVM>> GetCustomerAccounts(long? CustomerId)
        {
            long SiteId = await _workContext.GetCurrentSiteIdAsync();

            var query = _customerSimsRepository.Query()
                                                .Where(x => x.SiteId == SiteId &&
                                                      x.CustomerId == CustomerId &&
                                                        !x.IsDeleted)
                                                .AsNoTracking()
                                                .GroupBy(x => x.AccountId)
                                                .Select(x => new CustomerAccountsVM { AccountId = x.Key })
                                                .ToList();

            return query;

        }

        public async Task<PaginationResult<RequestChangePlanVM>> GetShechduledChangePlans(SmartTableParam param, long customerId)
        {
            var query = _changePlanRequestRepository.Query()
                                                        .Where(x => x.CreatedById == customerId && x.Status != ChangePlanStatus.Deleted && !x.CustomerSims.IsDeleted)
                                                        .Include(x => x.Product)
                                                        .Include(x => x.CustomerSims)
                                                        .ThenInclude(x => x.Product)
                                                        .AsNoTrackingWithIdentityResolution();

            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;
                if (search.ICCID != null)
                {
                    long ICCID = search.ICCID;
                    query = query.Where(x => x.CustomerSims.ICCID == ICCID);
                }


                if (search.Status != null)
                {
                    var status = (EIOTSIMStatus)search.Status;
                    query = query.Where(x => x.CustomerSims.Status == status);
                }


                if (search.ProductId != null)
                {
                    long ProductId = Convert.ToInt64(search.ProductId);
                    query = query.Where(x => x.ProductId == ProductId);
                }


                if (search.AccountId != null)
                {
                    long AccountId = Convert.ToInt64(search.AccountId);
                    query = query.Where(x => x.CustomerSims.AccountId == AccountId);
                }
                if (search.ProductName != null)
                {
                    string ProductName = search.ProductName;
                    ProductName = ProductName.ToUpper();

                    query = query.Where(x => x.Product.ProductDetails.Any(x=>x.NormalizedName.Contains(ProductName)));
                }

                if (search.Tag != null)
                {
                    string Tag = search.Tag;
                    Tag = Tag.ToLower();
                    query = query.Where(x => x.CustomerSims.SIMTag.ToLower().Contains(Tag));
                }

                if (search.SIMName != null)
                {
                    string SIMName = search.SIMName;
                    SIMName = SIMName.ToLower();
                    query = query.Where(x => x.CustomerSims.SIMName.ToLower().Contains(SIMName));
                }
            }



            var result = await query.OrderByDescending(x => x.CreationDate)
                                    .ProjectTo<RequestChangePlanVM>(mapper.ConfigurationProvider)
                                    .PaginateAsync(param.Pagination.Number, param.Pagination.Start);

            return result;

        }

        public async Task<SimsViewModel> GetSIM(long SIMId)
        {
            try
            {
                long cultureId = 0;
                var plattform = _workContext.GetCurrentPlatform();
                var SiteId = _workContext.GetCurrentSiteId();
                if (plattform == EPlatformSource.Admin)
                {
                    cultureId = _sitesCulturesRepository.Query().Where(x => x.CultureId == CultureId && x.SiteId == SiteId).FirstOrDefault().Id;
                }
                else
                {
                    cultureId = _workContext.GetCurrentCultureId();
                }

                
                var SIM = await _customerSimsRepository.Query()
                                                     .AsNoTrackingWithIdentityResolution()
                                                     .Where(x => x.Id == SIMId && SiteId == x.SiteId && !x.IsDeleted)
                                                     .Include(x => x.Product)
                                                     .ThenInclude(x=>x.ProductDetails)
                                                     .Select(x => new SimsViewModel
                                                     {

                                                         SIMName = x.SIMName,
                                                         ICCID = x.ICCID,
                                                         SIMTag = x.SIMTag,
                                                         Status = x.Status,
                                                         MSISDN = x.MSISDN,
                                                         IMSI = x.IMSI,
                                                         AccountId = x.AccountId,
                                                         APNType = x.APNType,
                                                         AssignIP = x.AssignIP,
                                                         City = x.City,
                                                         Id = x.Id,
                                                         ProductId = x.ProductId,
                                                         ProductName = x.Product.ProductDetails.Any(x => x.CultureId == cultureId) ?  x.Product.ProductDetails.Where(x=>x.CultureId== cultureId).FirstOrDefault().Name : "",
                                                         OrderId = x.OrderId

                                                     }).FirstOrDefaultAsync();
                if (SIM is null)
                {
                    throw new Exception("SIM Not Founded.");
                }
                return SIM;


            }
            catch (Exception ex)
            {

                throw ex;
            }


        }



        private CalculatedInvocies CalculateCustomerInvoice(List<CustomerInvoices> currentCustomerInvoices, List<CustomerInvoices> previousUnpaidInvoices)
        {

            var TotalSubAmount = _currencyService.DcimalRound(currentCustomerInvoices.Sum(x => x.SubAmount));

            var TotalAfterTax = _currencyService.DcimalRound(currentCustomerInvoices.Sum(x => x.TotalAmount));

            var TotalTaxAmount = _currencyService.DcimalRound(currentCustomerInvoices.Sum(x => x.TaxAmount.Value));

            var AllPreviousUnpaid = previousUnpaidInvoices.Any() ? previousUnpaidInvoices.Sum(x => x.TotalAmount) : 0;

            var AllDueAmount = _currencyService.DcimalRound(AllPreviousUnpaid + TotalSubAmount + TotalTaxAmount);

            var calculatedInovices = new CalculatedInvocies
            {
                TotalSubAmount = TotalSubAmount,
                TotalTaxAmount = TotalTaxAmount,
                AllPreviousUnpaid = AllPreviousUnpaid,
                AllDueAmount = AllDueAmount,
                TotalAfterTax = TotalAfterTax
            };

            return calculatedInovices;

        }

        private List<SummarySimInvoiceVM> GetSummarySimInvoices(List<CustomerInvoices> customerInvoices , long cultureId)
        {
            var summarySimInvoices = customerInvoices
                                       .GroupBy(x => x.ProductId)
                                       .Select(group => new
                                       {
                                           ProductId = group.Key,
                                           GroupItems = group.ToList()
                                       })
                                           .Select(x =>
                                           {
                                               var firstItem = x.GroupItems.FirstOrDefault();
                                               return new SummarySimInvoiceVM
                                               {
                                                   ServiceId = firstItem.ServiceId,
                                                   TotalAmount = _currencyService.DcimalRound(firstItem.TotalAmount) + " SAR",
                                                   TotalInvoicesAmount = _currencyService.DcimalRound(x.GroupItems.Sum(x => x.TotalAmount)) + " SAR",
                                                   TaxAmount = _currencyService.DcimalRound(firstItem.TaxAmount.Value) + " SAR",
                                                   SubAmount = _currencyService.DcimalRound(firstItem.SubAmount) + " SAR",
                                                   ServiceName = firstItem.Product.ProductDetails.Where(x=>x.CultureId== cultureId).FirstOrDefault()?.Name,
                                                   TaxRate = firstItem.TaxRate + "%",
                                                   StartDate = firstItem.StartDate.ToString("dd-MMM-yy"),
                                                   EndDate = firstItem.EndDate.ToString("dd-MMM-yy"),
                                                   CreationDate = firstItem.CreationDate.ToString("dd / MMM / yyyy"),
                                                   CustomerName = firstItem.Customer.FullName,
                                                   AccountId = firstItem.AccountId,
                                                   Quantity = x.GroupItems.Count,
                                                   Description = firstItem.Product.ProductDetails.Where(x => x.CultureId == cultureId).FirstOrDefault()?.ShortDescription

                                               };
                                           })
                                           .ToList();

            return summarySimInvoices;
        }

        private async Task<string> GetCustomerInvoiceTemplate(List<SummarySimInvoiceVM> summarySimInvoices)
        {
            const string CustomerTemplateId = "CustomerInvoiceTemplate";
            var siteId = await _workContext.GetCurrentSiteIdAsync();
            var settings = _siteSettingRepository.Query().AsNoTracking()
                                                    .Where(x => x.SiteId == siteId && x.Id == CustomerTemplateId)
                                                    .FirstOrDefault();

            var templateId = settings.Value;

            var template = await _emailTemplates.Query().AsNoTracking()
                                           .Where(x => x.Id == templateId && x.Status == EStatus.Active)
                                           .FirstOrDefaultAsync();

            var templateBody = template.EmailBody;
            string divPattern = @"<tr align=""center"">[\s\S]*?<\/tr>";


            Regex divRegex = new Regex(divPattern);
            string divToDuplicate = divRegex.Match(templateBody).Value;


            StringBuilder duplicatedDivs = new StringBuilder();

            foreach (var sim in summarySimInvoices)
            {
                var filledTemplateData = FillTemplateWithData(divToDuplicate, sim);
                duplicatedDivs.Append(filledTemplateData);
            }

            string finalResult = duplicatedDivs.ToString();

            string modifiedTemplate = templateBody.Replace($"{divToDuplicate}", finalResult);



            return modifiedTemplate;
        }

        #endregion


        #region Update


        public async Task<bool> UpdateBulkSims(UpdateBulkSimsVM model)
        {
            try
            {
                long siteId = await _workContext.GetCurrentSiteIdAsync();



                var simToUpdate = await _customerSimsRepository.Query()
                                                    .Where(x => x.SiteId == siteId && model.SimsId.Contains(x.Id) && !x.IsDeleted)
                                                    .ToListAsync();


                foreach (var sim in simToUpdate)
                {

                    if (!string.IsNullOrEmpty(model.SimName))
                    {
                        sim.SIMName = model.SimName;
                    }
                    if (!string.IsNullOrEmpty(model.SimTag))
                    {
                        sim.SIMTag = model.SimTag;
                    }

                }

                await _customerSimsRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception e)
            {

                throw;
            }

        }

        public async Task<bool> UpdateSIM(CustomerSimsUpdateViewModel SIM)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var TargetSIM = await _customerSimsRepository.Query().Where(x => x.Id == SIM.Id && !x.IsDeleted).FirstOrDefaultAsync();

                if (TargetSIM == null)
                {
                    throw new Exception("SIM Not Founded");

                }
                TargetSIM.MSISDN = SIM.MSISDN;
                if (TargetSIM.AssignIP != SIM.AssignIP)
                {
                    await _SIMsActionsService.UpdateSTCIPAddress(new SIMWithIdentifiers()
                    {
                        Id = TargetSIM.Id,
                        IdentifierType = TargetSIM.IdentifierType,
                        ICCID = TargetSIM.ICCID,
                        IMSI = TargetSIM.IMSI
                    }, SIM.AssignIP);


                    TargetSIM.AssignIP = SIM.AssignIP;

                }
                TargetSIM.IMSI = SIM.IMSI;

                TargetSIM.SIMName = SIM.SIMName;
                TargetSIM.ICCID = SIM.ICCID;
                TargetSIM.APNType = SIM.APNType;
                TargetSIM.City = SIM.City;
                TargetSIM.SIMTag = SIM.SIMTag;
                TargetSIM.IdentifierType = GetESIMsIdentifierType(SIM.IMSI, SIM.ICCID);

                TargetSIM.ModificationDate = DateTimeOffset.Now;
                TargetSIM.ModifiedById = UserId;

                ValidateSIM_ICCID_And_IMSI(TargetSIM);
                await _customerSimsRepository.SaveChangesAsync();
                CustomerSimsHistory History = new CustomerSimsHistory(TargetSIM, TargetSIM.Status, "Update SIM Info.", UserId);
                _customerSimsHistoryRepository.Add(History);
                await _customerSimsHistoryRepository.SaveChangesAsync();

                return true;



            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.ThrowDeepException(ex);
                if (ex.Message.Contains("IX_Integrations_CustomerSims_ICCID"))
                {
                    throw new Exception("Invalid Or Duplicated ICCID");
                }
                else if (ex.Message.Contains("IX_Integrations_CustomerSims_IMSI"))
                {
                    throw new Exception("Invalid Or Duplicated IMSI");
                }
                else
                {
                    throw ex;

                }
            }
        }


        #endregion


        #region Delete
        public async Task<bool> DeleteSIM(long SIMId)
        {
            try
            {


                var SiteId = _workContext.GetCurrentSiteId();
                var UserId = await _workContext.GetCurrentUserId();
                var SIM = await _customerSimsRepository.Query()
                                                     .Where(x => x.Id == SIMId && SiteId == x.SiteId)
                                                     .FirstOrDefaultAsync();
                if (SIM is null)
                {
                    throw new Exception("SIM Not Founded.");
                }
                SIM.Delete(UserId);

                await _customerSimsRepository.SaveChangesAsync();
                CustomerSimsHistory objCustomerSimsHistory = new CustomerSimsHistory(SIM, SIM.Status, "Delete SIM.", UserId);
                _customerSimsHistoryRepository.Add(objCustomerSimsHistory);
                await _customerSimsHistoryRepository.SaveChangesAsync();
                return true;


            }
            catch (Exception ex)
            {

                throw ex;
            }


        }

        public async Task<bool> DeleteRequstedPlan(long Id)
        {
            var newPlan = await _changePlanRequestRepository.Query()
                                                      .Where(x => x.Id == Id)
                                                      .FirstOrDefaultAsync();


            if (newPlan is null)
            {
                return false;
            }
            newPlan.Status = ChangePlanStatus.Deleted;
            await _changePlanRequestRepository.SaveChangesAsync();
            return true;

        }
        #endregion

        #region History

        public async Task<PaginationResult<SimsHistoryVM>> GetSIMHistory(SmartTableParam param)
        {
            try
            {
                long SiteId = await _workContext.GetCurrentSiteIdAsync();

                var query = _customerSimsHistoryRepository.Query()
                    .Where(x => !x.CustomerSims.IsDeleted)
                    .Include(x => x.Product)
                    .Include(x => x.CreatedBy)
                    .AsNoTrackingWithIdentityResolution();

                if (param.Search.PredicateObject != null)
                {
                    dynamic search = param.Search.PredicateObject;

                    if (search.OldStatus != null)
                    {
                        EIOTSIMStatus OldStatus = search.OldStatus;
                        query = query.Where(x => x.OldStatus == OldStatus);
                    }
                    if (search.NewStatus != null)
                    {
                        EIOTSIMStatus NewStatus = search.NewStatus;
                        query = query.Where(x => x.NewStatus == NewStatus);
                    }

                    if (search.AccountId != null)
                    {
                        long AccountId = search.AccountId;
                        query = query.Where(x => x.AccountId == AccountId);
                    }

                    if (search.SimId != null)
                    {
                        long SimId = Convert.ToInt64(search.SimId);
                        query = query.Where(x => x.CustomerSimsId == SimId);
                    }
                    if (search.From != null)
                    {

                        DateTimeOffset From = search.From;
                        query = query.Where(x => x.CreationDate >= From);
                    }
                    if (search.To != null)
                    {
                        DateTimeOffset To = search.To;
                        query = query.Where(x => x.CreationDate <= To);
                    }



                }

                var result = await query
                    .OrderByDescending(x => x.CreationDate)
                    .PaginateAsync(param.Pagination.Number, param.Pagination.Start);

                return new PaginationResult<SimsHistoryVM>(
                    mapper.Map<List<CustomerSimsHistory>, List<SimsHistoryVM>>(result.Data),
                    result.TotalPage,
                    result.TotalCount
                    );


            }
            catch (Exception exc)
            {

                throw exc;
            }
        }


        #endregion

        #region Helpers
        private void ValidateSIM_ICCID_And_IMSI(CustomerSims SIM)
        {
            if (!SIM.IMSI.HasValue && !SIM.ICCID.HasValue)
            {
                throw new Exception("IMSI Or ICCID Must Have A Value");
            }
        }


        private async Task<bool> ValidateCustomerSim(List<CustomerSimsVM> CustomerSims)
        {
            var AccountId = CustomerSims.FirstOrDefault().AccountId;
            var ValidateAccountId = CustomerSims.All(x => x.AccountId == AccountId);
            if (!ValidateAccountId)
                throw new Exception("All Of Sims Must Have The Same Account Id");


            return true;
        }

        private ESIMsIdentifiers GetESIMsIdentifierType(long? IMSI, long? ICCID)
        {
            if (ICCID != null && ICCID != 0 && IMSI != null && IMSI != 0)
            {
                return ESIMsIdentifiers.ANY;
            }
            else if (ICCID != null && ICCID != 0)
            {
                return ESIMsIdentifiers.ICCID;

            }
            else if (IMSI != null && IMSI != 0)
            {
                return ESIMsIdentifiers.IMSI;

            }
            throw new Exception("No Identitifire for this SIM.");
        }

        private void UpdateStatusPeriod(EIOTSIMStatus statusSnapshot, DateTimeOffset currentDate, DateTimeOffset startDate, List<Period> activePeriods, List<Period> terminatedPeriods, List<Period> suspendedPeriods, List<Period> waitingPeriods)
        {
            switch (statusSnapshot)
            {
                case EIOTSIMStatus.Active:
                    activePeriods.Add(new Period { From = startDate, To = currentDate });
                    break;

                case EIOTSIMStatus.Terminated:
                    terminatedPeriods.Add(new Period { From = startDate, To = currentDate });
                    break;

                case EIOTSIMStatus.Suspended:
                    suspendedPeriods.Add(new Period { From = startDate, To = currentDate });
                    break;

                case EIOTSIMStatus.WaitingForActivation:
                    waitingPeriods.Add(new Period { From = startDate, To = currentDate });
                    break;
            }
        }


        private static string FillTemplateWithData(string templateHtml, dynamic data)
        {
            // Replace placeholders in the template HTML with dynamic data
            foreach (var property in data.GetType().GetProperties())
            {
                // string placeholder = $"{{{property.Name}}}";
                string placeholder = $"${{{{{property.Name}}}}}";
                var value = Convert.ToString(property.GetValue(data));
                templateHtml = templateHtml.Replace(placeholder, value);
            }

            return templateHtml;
        }
        #endregion;
    }
}
