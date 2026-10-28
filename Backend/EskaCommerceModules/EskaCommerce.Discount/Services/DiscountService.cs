using EskaCMS.Core.Data;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.DataAccess.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Discount;
using EskaCMS.Entities.Entity.Pricing;
using EskaCMS.Entities.Entity.UserDashboard;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Extensions;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.Discount.Models;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Notifiers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Discount.Services
{
    public class DiscountService : IDiscountService
    {



        private readonly IRepository<EskaCMS.Entities.Entity.Discount.Discount> _discountRepository;
        private readonly IRepository<DiscountCustomerGroup> DiscountcustomergroupRepository;
        private readonly IRepository<DiscountProduct> discountproductRepository;
        private readonly IRepository<Product> productRepository;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly EskaDCMSDbContext _context;
        private readonly ISendDiscountSms sendSMs;
        private readonly IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository;
        private readonly ISiteSettingsService settingservice;
        private readonly IRepository<CustomerGroup> customerGroupRepository;
        private readonly IRepository<EskaCMS.Entities.Entity.Discount.DiscountTiers> _discountTiersRepository;
        private readonly IWorkContext _workContext;
        private readonly ITestNotifier testNotifier;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // HttpContext.Items key for the per-request discount lists (see GetDiscountsAsync).
        private static readonly object RequestDiscountsKey = new object();

        public DiscountService(
            IRepository<EskaCMS.Entities.Entity.Discount.Discount> discountRepository,
            IRepository<EskaCMS.Entities.Entity.Discount.DiscountCustomerGroup> discountcustomergroupRepository,
            IRepository<EskaCMS.Entities.Entity.Discount.DiscountProduct> discountproductRepository,
            IRepository<Product> ProductRepository,
            IServiceScopeFactory serviceScopeFactory,
            EskaDCMSDbContext context,
            ISendDiscountSms SendSMs,
                        IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository,
                                                                        ISiteSettingsService settingservice,

            IRepository<CustomerGroup> customerGroupRepository,
            IWorkContext workContext
            , ITestNotifier testNotifier,
            IRepository<EskaCMS.Entities.Entity.Discount.DiscountTiers> discountTiersRepository,
            IHttpContextAccessor httpContextAccessor


            )
        {
            _discountRepository = discountRepository;
            DiscountcustomergroupRepository = discountcustomergroupRepository;
            this.discountproductRepository = discountproductRepository;
            productRepository = ProductRepository;
            this.serviceScopeFactory = serviceScopeFactory;
            _context = context;
            sendSMs = SendSMs;
            this.siteSettingsRepository = siteSettingsRepository;
            this.settingservice = settingservice;
            this.customerGroupRepository = customerGroupRepository;
            _workContext = workContext;
            this.testNotifier = testNotifier;
            _discountTiersRepository = discountTiersRepository;
            _httpContextAccessor = httpContextAccessor;

        }
        public async Task<bool> UpsertDiscount(RequestDiscountViewModel model)
        {
            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var DBTrans = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        long userId = await _workContext.GetCurrentUserId();
                        long siteId = _workContext.GetCurrentSiteId();
                        bool isEditMode = model.Id != 0;

                        await ValidateDiscountPrice(model);

                        if (model.DiscountType == DiscountTypes.Tiers)
                        {
                            ValidateDiscountTiersRanges(model.DiscountTiers);
                        }

                        var Discount = await GetOrCreateDiscountAsync(model, siteId, isEditMode, userId);

                        UpdateDiscountProperties(Discount, model, siteId);

                        if (!isEditMode)
                        {
                            _discountRepository.Add(Discount);
                        }

                        await _discountRepository.SaveChangesAsync();

                        await InsertUpdatedDiscountProductsAsync(Discount.Id, model.Products.ToList());
                        await InsertUpdatedTiersAsync(Discount.Id, model.DiscountTiers);

                        await DBTrans.CommitAsync();

                        await InsertUpdateDiscountCustomerGroupsAsync(Discount.Id, model.CustomerGroups.ToList(), model.IsCustomerGroup);
                    }
                    catch (Exception e)
                    {
                        await DBTrans.RollbackAsync();
                        throw e;
                    }
                });

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
            finally
            {
                ForgetRequestDiscounts();
            }
        }



        public async Task<IList<DiscountVM>> GetDiscountsBySiteId(long SiteId)
        {
            try
            {
                return await _discountRepository.Query()
                    .Include(x => x.CustomerGroups)
                    .ThenInclude(x => x.CustomerGroup)
                    .Include(x => x.Products)
                    .ThenInclude(x => x.Product)
                    .ThenInclude(x => x.ProductDetails)
                    .AsNoTracking()
                    .Where(x => x.SiteId == SiteId && x.Status != EStatus.Deleted).Select(x => new DiscountVM
                    {
                        Description = x.Description,
                        SiteId = x.SiteId,
                        DiscountType = x.DiscountType,
                        DiscountTypeDesc = x.DiscountType.ToString(),
                        DiscountValue = x.DiscountValue,
                        Name = x.Name,
                        Id = x.Id,
                        StartTime =x.StartDate??null,
                        ExpiredTime =x.ExpiryDate??null,
                        IsCustomerGroup = x.IsCustomerGroup,
                        CustomerGroups = x.CustomerGroups.Select(d => d.CustomerGroup).Select(x => new CustomerGroupDiscountViewModel
                        {
                            Id = x.Id,
                            Name = x.customerGroupDetails.Select(y => y.Name).FirstOrDefault(),
                            IsActive = x.IsActive

                        }).ToList(),
                        Products = x.Products.Select(x => x.Product)
                                      .Select(x => new ProductDiscountViewModel
                                      {
                                          Id = x.Id,
                                          Name = x.ProductDetails.OrderByDescending(x => x.CultureId).Select(x => x.Name).FirstOrDefault(),
                                          IsPublished = x.Status == EskaCMS.Entities.Enum.GeneralEnums.EStatus.Published ? true : false
                                      }).ToList(),

                        IsProduct = x.Products.Any()

                    }
                  ).ToListAsync();
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        public async Task<DiscountVM> GetDiscount(long Id, long SiteId)
        {
            try
            {

                return await _discountRepository.Query()
                    .Where(x => x.SiteId == SiteId && x.Id == Id)
                    .Include(x => x.DiscountTiers)
                    .Include(x => x.CustomerGroups)
                    .ThenInclude(x => x.CustomerGroup)
                    .ThenInclude(x => x.customerGroupDetails)
                    .Include(x => x.Products)
                    .ThenInclude(x => x.Product)
                    .ThenInclude(x => x.ProductDetails)

                    .Select(x => new DiscountVM
                    {

                        Description = x.Description,
                        DiscountType = x.DiscountType,
                        DiscountTiers = x.DiscountTiers,
                        DiscountValue = x.DiscountValue,
                        Name = x.Name,
                        Id = x.Id,
                        IsCustomerGroup = x.IsCustomerGroup,
                        StartTime = x.StartDate ?? null,
                        ExpiredTime = x.ExpiryDate ?? null,
                        IsProduct = x.Products.Any(),
                        CustomerGroups = x.CustomerGroups.Select(d => d.CustomerGroup).Select(x => new CustomerGroupDiscountViewModel
                        {
                            Id = x.Id,
                            Name = x.customerGroupDetails.Select(y => y.Name).FirstOrDefault()

                        }).ToList(),
                        Products = x.Products.Select(x => x.Product)
                                      .Select(x => new ProductDiscountViewModel
                                      {
                                          Id = x.Id,
                                          Name = x.ProductDetails.OrderByDescending(x => x.CultureId).Select(x => x.Name).FirstOrDefault(),
                                          IsPublished = x.Status == EskaCMS.Entities.Enum.GeneralEnums.EStatus.Published ? true : false
                                      }).ToList(),
                    }
                  ).FirstOrDefaultAsync();

            }
            catch (Exception ex)
            {

                throw;
            }
        }
        public async Task<bool> Delete(long id)
        {
            try
            {
                var disc = await _discountRepository.Query()
                    .Include(x => x.DiscountTiers)
                    .Include(x => x.Products)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (disc == null)
                {
                    throw new Exception("Not found");
                }

                var CustomerGroup = await DiscountcustomergroupRepository.Query().Where(x => x.DiscountId == id).ToListAsync();

                if (CustomerGroup.Count() > 0)
                {
                    DiscountcustomergroupRepository.RemoveRange(CustomerGroup);
                    await DiscountcustomergroupRepository.SaveChangesAsync();
                }
                var ProductsDiscounts = await discountproductRepository.Query().Where(x => x.DiscountId == id).ToListAsync();

                if (ProductsDiscounts.Count() > 0)
                {
                    discountproductRepository.RemoveRange(ProductsDiscounts);
                    await discountproductRepository.SaveChangesAsync();
                }
                disc.Status = EStatus.Deleted;

                await _discountRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception exc)
            {
                throw;
            }
            finally
            {
                ForgetRequestDiscounts();
            }
        }
        //}

   

        #region Helpers To Add And Update And Get And Send Notification
        private async Task<NotificationObject> HandlerNotification(List<string> PhoneNumbers, EskaCMS.Entities.Entity.Discount.Discount discount)
        {
            try
            {
                var userId = await _workContext.GetCurrentUserId();
                var siteId = _workContext.GetCurrentSiteId();
                var CultureId = await GetDefaultCulture();
                var TemplateId = await siteSettingsRepository.Query().Where(x => x.Id == "DiscountTemplate").Select(x => x.Value).FirstOrDefaultAsync();
                NotificationObject data = new NotificationObject();
                List<long> Ids = new List<long>();
                List<string> SendValues = new List<string>();
                Ids.Add(userId);
                SendValues.AddRange(PhoneNumbers);
                data.UserId = Ids.ToArray();
                data.MessageTitle = "";
                data.MessageBody = "";
                data.SendValue = SendValues;
                var defaultSmsProvider = (await settingservice.GetSiteSettingById("SmsProvider", siteId))?.Value;


                if (defaultSmsProvider is null)
                    throw new Exception("Please add default provider");

                var smsProvider = (SMSProviders)Enum.Parse(typeof(SMSProviders), defaultSmsProvider, true);
                data.EmailOptions = new EskaCMS.EmailSender.SMTP.Areas.ViewModels.EmailSenderVM
                {
                    TemplateId = TemplateId,
                    Params = new List<EskaCMS.EmailSender.SMTP.Areas.ViewModels.EmailParamsVM>() {

                                new EskaCMS.EmailSender.SMTP.Areas.ViewModels.EmailParamsVM{ Param="[DiscountName]",Value=discount.Name},
                            new EskaCMS.EmailSender.SMTP.Areas.ViewModels.EmailParamsVM{ Param="[DiscountValue]",
                                Value= discount.DiscountValue.ToString() + (discount.DiscountType == DiscountTypes.Percentage
                                                                            ?" %":"")}

                    },
                    Emails = SendValues

                };
                data.SendVia = SendBy.MobileNumber;
                data.NotificationType = NotificationType.SMS;
                data.SMSProvider = SMSProviders.SMSMPP;
                data.Severity = 0;

                return data;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<long> GetDefaultCulture()
        {
            try
            {
                var CultureId = await siteSettingsRepository.Query().Where(x => x.Id == "DefaultCulture").Select(x => x.Value).FirstOrDefaultAsync();

                return Convert.ToInt64(CultureId);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private async Task InsertUpdatedTiersAsync(long DiscountId, IList<RequestDiscountTiersViewModel> discountTiers)
        {
            try
            {
                var AddDiscountTiers = new List<DiscountTiers>();

                var DiscountTiers = await _discountTiersRepository.Query().Where(x => x.DiscountId == DiscountId).ToListAsync();
                if (DiscountTiers.Any())
                {
                    _discountTiersRepository.RemoveRange(DiscountTiers);
                    await _discountTiersRepository.SaveChangesAsync();
                }

                //    ValidateDiscountTiers(discountTiers);
                // Current user is invariant for the whole request; fetch it once instead of once per tier.
                var currentUserId = await _workContext.GetCurrentUserId();
                foreach (var item in discountTiers)
                {
                    var discTiers = new DiscountTiers
                    {
                        DiscountId = DiscountId,
                        CreationDate = DateTimeOffset.Now,
                        CreatedById = currentUserId,
                        DiscountValue = item.DiscountValue,
                        DiscountTierType = item.DiscountTierType,
                        FromValue = item.FromValue,
                        ToValue = item.ToValue,
                    };
                    AddDiscountTiers.Add(discTiers);

                }

                if (AddDiscountTiers.Any())
                {

                    _discountTiersRepository.AddRange(AddDiscountTiers);
                    await _discountTiersRepository.SaveChangesAsync();

                }
               

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<bool> InsertUpdateDiscountCustomerGroupsAsync(long DiscountId, List<long>? customerGroupIds, bool IsCustomerGroup)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var Discount = await _discountRepository.Query()
                                                        .Where(x => x.Id == DiscountId)
                                                        .Include(x => x.Products)
                                                        .ThenInclude(x => x.Product)
                                                        .ThenInclude(x => x.ProductDetails)
                                                        .Include(x => x.DiscountTiers)
                                                        .FirstOrDefaultAsync();
                var OriginalDiscountGroups = await DiscountcustomergroupRepository.Query().Where(x => x.DiscountId == DiscountId).ToListAsync();


                var discountCustomerGroup = new List<DiscountCustomerGroup>();
                if (OriginalDiscountGroups.Count > 0)
                {
                    DiscountcustomergroupRepository.RemoveRange(OriginalDiscountGroups);
                }
                if (IsCustomerGroup && customerGroupIds.Count() > 0)
                {
                    discountCustomerGroup = customerGroupIds.ConvertAll(y => new DiscountCustomerGroup { DiscountId = DiscountId, CustomerGroupId = y });
                    DiscountcustomergroupRepository.AddRange(discountCustomerGroup);
                }
                await DiscountcustomergroupRepository.SaveChangesAsync();

                if (discountCustomerGroup.Count > 0)
                {

                    var customerGroup = await customerGroupRepository
                                                                      .Query()
                                                                      .Include(x => x.Users)
                                                                      .ThenInclude(x => x.User)
                                                                      .Where(x => customerGroupIds.Contains(x.Id))
                                                                      .ToListAsync();

                    var PhoneNumbers = customerGroup.SelectMany(x => x.Users)
                                                    .Select(x => x.User)
                                                    .Where(x => x.Status != EStatus.Deleted && !string.IsNullOrEmpty(x.PhoneNumber))
                                                    .Select(x => x.PhoneNumber)
                                                    .ToList();

                    //NotificationObject notificationObject = await HandlerNotification(PhoneNumbers, Discount);

                    //Task.Run(async () =>
                    //{
                    //    try
                    //    {
                    //        await sendSMs.SendMessageAsync(NotificationType.SMS, null, notificationObject, SiteId);
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        throw ex;
                    //    }
                    //}).ConfigureAwait(false);

                }


                return true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("An error occurred while saving the entity changes. See the inner exception for details."))
                {

                    throw new Exception(ex.InnerException.Message);
                }

                throw ex;
            }
        }

        private async Task InsertUpdatedDiscountProductsAsync(long DiscountId, List<long>? productsId)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var DiscountExist = await _discountRepository.Query().Where(x => x.Id == DiscountId).FirstOrDefaultAsync();
                var OriginalDiscountProducts = await discountproductRepository.Query().Where(x => x.DiscountId == DiscountId).ToListAsync();

                var AllProductsDiscounts = await discountproductRepository.Query().Where(x => x.DiscountId != DiscountId).Select(x => x.ProductId).ToListAsync();

                foreach (var discountproduct in AllProductsDiscounts)
                {
                    if (productsId.Any(x => x == discountproduct))
                    {
                        throw new Exception("the product have another discount");
                    }
                }

                var discountProducts = new List<DiscountProduct>();
                discountproductRepository.RemoveRange(OriginalDiscountProducts);
                if (productsId.Count > 0)
                {
                  
                        var DiscountProduct = productsId.Select(ProductId => new DiscountProduct { DiscountId = DiscountId, ProductId = ProductId }).ToList();
                        discountproductRepository.AddRange(DiscountProduct);
                    
                }

                await discountproductRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private void UpdateDiscountProperties(EskaCMS.Entities.Entity.Discount.Discount discount, RequestDiscountViewModel model, long siteId)
        {
            try
            {
                discount.Name = model.Name;
                discount.Description = model.Description;
                discount.DiscountType = model.DiscountType;
                discount.DiscountValue = model.DiscountValue;
                discount.StartDate = model.StartTime;
                discount.ExpiryDate = model.ExpiredTime;
                discount.SiteId = siteId;
                discount.IsCustomerGroup = model.CustomerGroups.Any() && model.IsCustomerGroup ? true : false;
                discount.Status = EStatus.Published;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<EskaCMS.Entities.Entity.Discount.Discount> GetOrCreateDiscountAsync(RequestDiscountViewModel model, long siteId, bool isEditMode, long userId)
        {
            try
            {
                if (isEditMode)
                {
                    var discount = await _discountRepository
                                                  .Query()
                                                  .Where(m => m.Id == model.Id && m.SiteId == siteId)
                                                  .Include(x => x.CustomerGroups)
                                                  .ThenInclude(x => x.CustomerGroup)
                                                  .ThenInclude(x => x.Users)
                                                  .Include(x => x.Products)
                                                  .ThenInclude(x => x.Product)
                                                  .FirstOrDefaultAsync();

                    if (discount == null)
                    {
                        throw new Exception("Not Found Discount");
                    }

                    discount.ModifiedById = userId;
                    discount.ModificationDate = DateTimeOffset.Now;

                    return discount;
                }

                return new EskaCMS.Entities.Entity.Discount.Discount
                {
                    CreatedById = userId,
                    CreationDate = DateTimeOffset.Now
                };

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<List<EskaCMS.Entities.Entity.Discount.Discount>> GetDiscountsAsync(long siteId)
        {
            // Pricing a cart calls this twice per line plus once for the global discount, and one checkout
            // prices the cart several times, so this whole graph used to be reloaded dozens of times per
            // request. It is loaded without tracking and only read by CalculateDiscount and
            // CalculateGlobalDiscount, so it is loaded once per site per HTTP request and reused.
            // Outside a request (no HttpContext) nothing is cached.
            var requestItems = _httpContextAccessor?.HttpContext?.Items;
            Dictionary<long, List<EskaCMS.Entities.Entity.Discount.Discount>> requestDiscounts = null;

            if (requestItems != null)
            {
                if (requestItems.TryGetValue(RequestDiscountsKey, out var cached))
                {
                    requestDiscounts = (Dictionary<long, List<EskaCMS.Entities.Entity.Discount.Discount>>)cached;

                    if (requestDiscounts.TryGetValue(siteId, out var discounts))
                    {
                        return discounts;
                    }
                }
                else
                {
                    requestDiscounts = new Dictionary<long, List<EskaCMS.Entities.Entity.Discount.Discount>>();
                    requestItems[RequestDiscountsKey] = requestDiscounts;
                }
            }

            var loaded = await _discountRepository.Query()
                .AsNoTrackingWithIdentityResolution()
                .Where(x => x.SiteId == siteId && x.Status != EStatus.Deleted)
                .Include(x => x.CustomerGroups)
                .ThenInclude(x => x.CustomerGroup)
                .ThenInclude(x => x.Users)
                .Include(x => x.DiscountTiers)
                .Include(x => x.Products)
                .ThenInclude(x => x.Product)
                .Include(x => x.PlansVersions)
                .ToListAsync();

            if (requestDiscounts != null)
            {
                requestDiscounts[siteId] = loaded;
            }

            return loaded;
        }

        // Discount writes must not be followed by stale per-request discount lists.
        private void ForgetRequestDiscounts()
        {
            _httpContextAccessor?.HttpContext?.Items.Remove(RequestDiscountsKey);
        }

        private async Task<EskaCMS.Entities.Entity.Discount.Discount> GetDiscountById(long DiscountId)
        {
            return await _discountRepository.Query()
                .AsNoTrackingWithIdentityResolution()
                .Include(x => x.CustomerGroups)
                .ThenInclude(x => x.CustomerGroup)
                .ThenInclude(x => x.Users)
                .Include(x => x.DiscountTiers)
                .Include(x => x.Products)
                .ThenInclude(x => x.Product)
                .Where(x => x.Id == DiscountId && x.Status != EStatus.Deleted)
                .FirstOrDefaultAsync();
        }

        private async Task ValidateDiscountPrice(RequestDiscountViewModel discountRequest)
        {
            try
            {
                if (IsFixedAmountDiscount(discountRequest) && HasEligibleProducts(discountRequest))
                {
                    var eligibleProducts = await GetEligibleProducts(discountRequest);

                    var hasInvalidPrice = eligibleProducts.Any(x =>
                    {
                        var minPrice = GetEffectiveMinPrice(x);
                        return minPrice < discountRequest.DiscountValue;
                    });

                    if (hasInvalidPrice)
                        throw new Exception("Product Price Lower Than Discount");
                }

                if (discountRequest.DiscountType == DiscountTypes.Tiers && HasEligibleProducts(discountRequest))
                {
                    await ValidateProductsWithinTierRange(discountRequest);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task ValidateProductsWithinTierRange(RequestDiscountViewModel discountRequest)
        {
            if (discountRequest.DiscountTiers == null || !discountRequest.DiscountTiers.Any())
                throw new Exception("At least one tier is required");

            var eligibleProducts = await GetEligibleProducts(discountRequest);

            foreach (var product in eligibleProducts)
            {
                var price = GetEffectiveMinPrice(product);

                bool isWithinAnyTier = discountRequest.DiscountTiers.Any(tier =>
                    price > tier.FromValue && price <= tier.ToValue);

                if (!isWithinAnyTier)
                {
                    throw new Exception(
                        $"Product price ({price}) does not fall within any defined tier range.");
                }
            }
        }
        private decimal? GetEffectiveMinPrice(Product product)
        {
            if (product.PriceType == EPriceType.WarehouseBase)
            {
                return product.ProductPriceWarehouses != null && product.ProductPriceWarehouses.Any()
                    ? product.ProductPriceWarehouses.Min(w => w.Price)
                    : product.Price; 
            }

            return product.Price;
        }
        private bool IsFixedAmountDiscount(RequestDiscountViewModel discountRequest)
        {
            return discountRequest.DiscountType == DiscountTypes.FixedAmount;
        }

        private bool HasEligibleProducts(RequestDiscountViewModel discountRequest)
        {
            return discountRequest.Products.Count > 0;
        }

        private async Task<List<Product>> GetEligibleProducts(RequestDiscountViewModel discountRequest)
        {
            return await productRepository.Query()
                .AsNoTracking()
                .Include(x=>x.ProductPriceWarehouses)
                .Where(x => discountRequest.Products.Contains(x.Id) && !x.IsPlan)
                .ToListAsync();
        }

        #endregion

        #region Function To Invoke Calculate Discount

        public async Task<decimal> CalculateDiscount(long siteId, decimal tiersAmount, long productId, long userId)
        {
            decimal Discount = 0;

            var Discounts = await GetDiscountsAsync(siteId);
            Discounts = Discounts.Where(x => x.Products.Count() > 0).ToList();

            foreach (var discount in Discounts)
            {
                if (discount.IsCustomerGroup && discount.CustomerGroups.Any())
                {
                    var Users = discount.CustomerGroups.SelectMany(x => x.CustomerGroup.Users).Select(x => x.UserId).ToList();
                    if (Users.Any(x => x == userId))
                    {
                        if (discount.Products.Any(x => x.ProductId == productId && !x.Product.IsPlan))
                        {
                            Discount += CalculateDiscountType(tiersAmount, discount);

                        }
                    }
                }
                else
                {

                    if (discount.Products.Any(x => x.ProductId == productId))
                    {
                        Discount += CalculateDiscountType(tiersAmount, discount);

                    }

                }
            }

            return Discount;
        }
        public async Task<decimal> CalculateGlobalDiscount(long siteId, decimal tiersAmount)
        {
            try
            {
                var Discounts = await GetDiscountsAsync(siteId);
                Discounts = Discounts.Where(x => x.Products.Count() == 0 && x.PlansVersions.Count() == 0).ToList();

                decimal GlobalDiscount = 0;
                foreach (var discount in Discounts)
                {
                    var calculatedDiscount = CalculateDiscountType(tiersAmount, discount);
                    if (calculatedDiscount != null)
                    {
                        GlobalDiscount += calculatedDiscount;
                    }
                }

                return GlobalDiscount;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<decimal> CalculatePlanDiscount(long DicountId, decimal tiersAmount)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var Discount = await GetDiscountById(DicountId);

                decimal Result = 0;

                if (Discount.IsCustomerGroup && Discount.CustomerGroups.Any())
                {
                    var Users = Discount.CustomerGroups.SelectMany(x => x.CustomerGroup.Users).Select(x => x.UserId).ToList();
                    if (Users.Any(x => x != UserId))
                    {
                        return 0;

                    }
                }
                var calculatedDiscount = CalculateDiscountType(tiersAmount, Discount);
                if (calculatedDiscount != null)
                {
                    Result += calculatedDiscount;
                }

                return Result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void ValidateDiscountTiersRanges(IList<RequestDiscountTiersViewModel> discountTiers)
        {

            foreach (var tier in discountTiers)
            {
                if (tier.FromValue < 0 || tier.ToValue < 0)
                    throw new Exception("Tier values cannot be negative");

                if (tier.FromValue >= tier.ToValue)
                    throw new Exception($"Tier range is invalid: From ({tier.FromValue}) must be less than To ({tier.ToValue})");
            }

            var orderedTiers = discountTiers.OrderBy(t => t.FromValue).ToList();

            for (int i = 0; i < orderedTiers.Count - 1; i++)
            {
                var current = orderedTiers[i];
                var next = orderedTiers[i + 1];

                if (next.FromValue < current.ToValue)
                {
                    throw new Exception(
                        $"Tier ranges overlap: [{current.FromValue}-{current.ToValue}] and [{next.FromValue}-{next.ToValue}]");
                }
            }
        }
        private decimal CalculateDiscountType(decimal tiersAmount, EskaCMS.Entities.Entity.Discount.Discount discountInfo)
        {
            switch (discountInfo.DiscountType)
            {
                case GeneralEnums.DiscountTypes.Percentage:
                    return tiersAmount * discountInfo.DiscountValue / 100;

                case GeneralEnums.DiscountTypes.FixedAmount:
                    return Convert.ToDecimal(discountInfo.DiscountValue);

                case GeneralEnums.DiscountTypes.Tiers:
                    if (discountInfo.DiscountTiers != null)
                    {
                        decimal tierDiscountValue = 0;

                        foreach (var tier in discountInfo.DiscountTiers)
                        {
                            if (tiersAmount >= tier.FromValue && tiersAmount < tier.ToValue)
                            {
                                if (tier.DiscountTierType == DiscountTiersTypes.FixedAmount)
                                {
                                    tierDiscountValue = tierDiscountValue + tier.DiscountValue;
                                }
                                else if (tier.DiscountTierType == DiscountTiersTypes.Percentage)
                                {
                                    var tiersPercentageAmount = tiersAmount * tier.DiscountValue / 100;

                                    tierDiscountValue = tierDiscountValue + tiersPercentageAmount;

                                }
                            }
                        }
                        return tierDiscountValue;
                    }
                    break;
            }

            return 0;
        }
        #endregion


    }
}
