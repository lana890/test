using EskaCMS.Core.Areas.Core.Interfaces;
using EskaCMS.Core.BusinessModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Interfaces;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Payments;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Entities.Enum;
using EskaCMS.Entities.Helpers;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Helpers.HttpHelper;
using EskaCMS.Integrations.Helpers;
using EskaCMS.Integrations.Models;
using EskaCMS.Integrations.Models.Insurance;
using EskaCMS.Integrations.Services.Interfaces;
using EskaCMS.Localization.Services;
using EskaCMS.Localization.Services.Interfaces;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Notifiers;
using EskaCommerce.Orders.Areas.Orders.ViewModels;
using EskaCommerce.Orders.Services;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels;
using EskaCommerce.ShoppingCart.Services;
using Google.Apis.Storage.v1.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Integrations.Services.InsuranceInterfaces
{
    
    public class IntegrationService : IIntegrationServices
    {
        private readonly IGeneralApiClient _generalApi;
        private readonly IMedicalApiClient _medicalApi;
        private readonly IFinancialApiClient _financialApi;

        private readonly IConfiguration configuration;
        private readonly IdcmsLogService dcmsLogService;
        private readonly ISiteCultureService siteCultureService;
        private readonly IUserSiteService userSiteService;
        private readonly IRepository<Order> orderRepository;
        private readonly IUserService userService;
        private readonly UserManager<User> userManager;
        private readonly IWorkContext _workContext;
        private readonly IOrderService _orderService;
        private readonly IRepository<CustomerGroup> customerGrouprepository;
        private readonly ICurrencyService currencyService;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IRepository<CartItem> _cartItemRepository;
        private readonly ILocalizationServices localizationServices;
        private readonly IRepositoryWithTypedId<SiteSettings, string> appSettingRepository;
        private readonly IRepository<Cart> _cartContext;
        private readonly ICartService _ICartService;
        private readonly ITestNotifier testNotifier;
        private readonly IRepository<User> userRepository;
        private readonly ISiteSettingsService siteSettings;
		private readonly IRepository<Cart> _cartRepository;
        private readonly IRepository<Payment> _paymentRepository;
        public IntegrationService(
            IRepository<Cart> cartRepository,
            IRepository<Payment> paymentRepository,

			IGeneralApiClient generalApi,
            IMedicalApiClient medicalApi,
            IFinancialApiClient financialApi,
            IRepository<Cart> cartContext,
            IRepository<CartItem> cartItemRepository,
            ILocalizationServices localizationServices,
            IRepositoryWithTypedId<SiteSettings, string> appSettingRepository,
            IConfiguration configuration,
            IdcmsLogService dcmsLogService,
            ISiteCultureService siteCultureService,
            IUserSiteService userSiteService,
            IRepository<Order> orderRepository,
            IUserService userService,
            UserManager<User> userManager,
            IWorkContext workContext,
            IOrderService orderService,
            IRepository<CustomerGroup> customerGrouprepository,
            ICartService ICartService,
            ITestNotifier testNotifier,
            IRepository<User> userRepository,
            ISiteSettingsService siteSettings,
            IHttpContextAccessor httpContext,
            ICurrencyService currencyService)
        {
            _generalApi   = generalApi;
            _medicalApi   = medicalApi;
            _financialApi = financialApi;
            _cartRepository = cartRepository;
            _paymentRepository = paymentRepository;

			this.configuration          = configuration;
            this.dcmsLogService         = dcmsLogService;
            this.siteCultureService     = siteCultureService;
            this.userSiteService        = userSiteService;
            this.orderRepository        = orderRepository;
            this.userService            = userService;
            this.userManager            = userManager;
            this.appSettingRepository   = appSettingRepository;
            _workContext                = workContext;
            _orderService               = orderService;
            this.customerGrouprepository = customerGrouprepository;
            _httpContext                = httpContext;
            _cartContext                = cartContext;
            _cartItemRepository         = cartItemRepository;
            this.localizationServices   = localizationServices;
            _ICartService               = ICartService;
            this.testNotifier           = testNotifier;
            this.userRepository         = userRepository;
            this.siteSettings           = siteSettings;
            this.currencyService        = currencyService;
        }


        public async Task CreatePolicyAsync(long orderId)
        {
            var siteId = await _workContext.GetCurrentSiteIdAsync()
                .WaitAsync(TimeSpan.FromSeconds(10));
			var order = await GetOrderDetails(orderId);

			var orderObj = order?.Cart?.Items?.FirstOrDefault();
			if (orderObj == null)
				throw new Exception("Order cart item not found");

			var ctx = BuildOrderCartContext(orderObj.AdditionalDetails);

			if (string.IsNullOrWhiteSpace(ctx.ProductCode))
                throw new Exception("ProductCode is missing in AdditionalDetails");
            if (string.IsNullOrWhiteSpace(ctx.NationalId))
                throw new Exception("NationalId is missing in AdditionalDetails");
            if (string.IsNullOrWhiteSpace(ctx.DocumentId))
                throw new Exception("DocumentId/PolicyID is missing in AdditionalDetails");

            await LogInfo("Policy.CreatePolicyAsync",
                $"order {orderId} product '{ctx.ProductCode}' document {ctx.DocumentId}", siteId);

            // ── Customer: get or create ── (each call guarded with a timeout to avoid hanging)
            var customerId = await RunCritical("GetCustomerByNationalId", siteId,
                _financialApi.GetCustomerByNationalId(ctx.NationalId));
            if (customerId == 0)
                customerId = await RunCritical("InsertCustomer", siteId,
                    _financialApi.InsertCustomer(BuildInsertCustomerPayload(ctx.Root, ctx.Email)));
            else
                await RunCritical("UpdateCustomer", siteId,
                    _financialApi.UpdateCustomer(BuildUpdateCustomerPayload(ctx.Root, customerId, ctx.Email)));

            // ── Route by product type ──
            switch (ctx.ProductCode?.ToLower())
            {
                case "travel":
                case "pa":
				case "motor":
				case "general":
					await HandleGeneralPolicyAsync(ctx, order, siteId, customerId);
					break;
                case "medical":
					await HandleMedicalPolicyAsync(ctx, order, siteId, customerId);
					break;
                default:
                    await LogError("CreatePolicyAsync",
                        new InvalidOperationException(
                            $"Unhandled ProductCode '{ctx.ProductCode}' — no policy created for order {orderId}."),
                        siteId);
                    break;
            }
        

		}


        // Smart repush for stuck orders:
        //  1) only proceed if the order is actually paid in the Payment table;
        //  2) if the policy was already created on a previous attempt (PolicyCode saved on the cart item)
        //     but never mapped, run MapOrderReference only — do NOT re-create the policy (would duplicate it);
        //  3) otherwise run the full CreatePolicyAsync journey;
        //  4) make the order status reflect the paid state.
        public async Task<RepushResultVM> RepushPolicyAsync(long orderId)
        {
            try
            {
                var order = await GetOrderDetails(orderId);
                if (order == null)
                    return new RepushResultVM { OrderId = orderId, Success = false, Action = "OrderNotFound" };

                // Guard: never create a policy for an order that was not paid.
                if (!await IsOrderPaidAsync(orderId))
                    return new RepushResultVM { OrderId = orderId, Success = false, Action = "SkippedNotPaid" };

                string action;

                if (!string.IsNullOrWhiteSpace(order.OrderReferanceId))
                {
                    // Already mapped — nothing to create or map again.
                    action = "AlreadyMapped";
                }
                else
                {
                    var policyCode = GetPolicyCodeFromCart(order);
                    if (!string.IsNullOrWhiteSpace(policyCode))
                    {
                        // Policy exists from a previous attempt — map only, don't duplicate it.
                        await MapOrderReference(policyCode, order);
                        action = "MappedExisting";
                    }
                    else
                    {
                        // No policy yet — run the full journey.
                        await CreatePolicyAsync(orderId);
                        action = "Created";
                    }
                }

                // Payment table says paid → make the order reflect it.
                await EnsureOrderPaidAsync(order);

                return new RepushResultVM { OrderId = orderId, Success = true, Action = action };
            }
            catch (Exception ex)
            {
                await LogError("RepushPolicyAsync", ex);
                return new RepushResultVM { OrderId = orderId, Success = false, Action = "Error", Error = ex.Message };
            }
        }

        private async Task<bool> IsOrderPaidAsync(long orderId)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            return await _paymentRepository.Query()
                .AsNoTracking()
                .AnyAsync(p => p.OrderId == orderId && p.Status == PaymentStatus.Succeeded, cts.Token);
        }

        private string GetPolicyCodeFromCart(Order order)
        {
            try
            {
                var cartObj = order.Cart?.Items?.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(cartObj?.AdditionalDetails))
                    return null;
                return JObject.Parse(cartObj.AdditionalDetails).GetStringSafe("PolicyCode");
            }
            catch { return null; }
        }

        private async Task EnsureOrderPaidAsync(Order order)
        {
            try
            {
                if (order.OrderStatus == OrderStatus.PaymentReceived)
                    return;
                await _orderService.OrderChangeStatus(order.Id, OrderStatus.PaymentReceived, "Repush", order.SiteId)
                    .WaitAsync(TimeSpan.FromSeconds(30));
            }
            catch (Exception ex)
            {
                await LogError("RepushPolicyAsync.EnsureOrderPaid", ex, order.SiteId);
            }
        }

        private async Task HandleGeneralPolicyAsync(
            OrderCartContext ctx, Order order, long siteId, long customerId)
        {
            ConvertToPolicyResponse policy = null;
            Exception pipelineError = null;

            // The pipeline is CRITICAL, but we capture its failure instead of letting
            // it escape immediately: ConvertToPolicy may well have created the policy
            // on the remote side before a later step (or our own 55 s ceiling) blew up.
            // If a policy came back we MUST finalise it, otherwise the customer is
            // charged and holds a policy the order never records.
            try
            {
                policy = await ProcessTravelAndPAFlow(
                    customerId, ctx.Root, ctx.DocumentId, ctx.GeneralRequest, siteId);
            }
            catch (Exception ex)
            {
                pipelineError = ex;
                await LogError("HandleGeneralPolicyAsync.ProcessTravelAndPAFlow", ex, siteId);
            }

            var generalPolicy = policy?.GeneralPolicy;

            if (generalPolicy == null)
            {
                // No policy exists — nothing to finalise. Surface the failure so the
                // caller marks the order PaymentFailed rather than PaymentReceived.
                await LogError("HandleGeneralPolicyAsync",
                    new InvalidOperationException(
                        "No GeneralPolicy was produced — order NOT finalised."), siteId);

                throw pipelineError
                    ?? new InvalidOperationException("ProcessTravelAndPAFlow returned a null GeneralPolicy.");
            }

            // ── Policy EXISTS from here on. Every step below is individually
            // guarded so no single failure can stop the ones after it. ──
            await LogInfo("Policy.Finalise",
                $"policy {generalPolicy.DocumentNo} created for order {order.Id} — finalising", siteId);

            if (pipelineError != null)
                await LogInfo("Policy.Finalise",
                    "pipeline reported an error but a policy was returned; finalising anyway", siteId);

            var quotationDate = ctx.Root.GetDateSafe("quotationDate") ?? DateTime.Now;
            await RunSafe("GenerateReceipt", siteId, _financialApi.GenerateReceipt(
                generalPolicy.DocumentNo.ToString(),
                policy?.TransactionId,
                generalPolicy.NetPremium.ToString(),
                quotationDate.ToString("dd/MM/yyyy")));

            await RunSafe("SavePolicyId", siteId, SavePolicyId(generalPolicy, order));

            await RunSafe("RegisterAndMapUser", siteId,
                TryRegisterAndMapUserAsync(ctx, order, siteId, customerId));

            if (!string.IsNullOrWhiteSpace(generalPolicy.SegmentCode))
                await RunSafe("MapOrderReference", siteId,
                    MapOrderReference(generalPolicy.SegmentCode, order));
            else
                await LogError("Policy.MapOrderReference",
                    new InvalidOperationException("SegmentCode is missing; order reference not saved."), siteId);

            await RunSafe("SendReceipt", siteId, SendReceipt(order.Id));

            await LogInfo("Policy.Finalise", $"order {order.Id} finalised", siteId);
        }

        private async Task HandleMedicalPolicyAsync(
            OrderCartContext ctx, Order order, long siteId, long customerId)
        {
            ConvertQuotationARTResponseVm result = null;
            Exception pipelineError = null;

            // Same contract as the general flow: capture the failure so a policy that
            // WAS created still gets finalised.
            try
            {
                result = await ProcessMedicalFlow(
                    customerId, ctx.Root, ctx.DocumentId, ctx.GeneralRequest, siteId, ctx);
            }
            catch (Exception ex)
            {
                pipelineError = ex;
                await LogError("HandleMedicalPolicyAsync.ProcessMedicalFlow", ex, siteId);
            }

            var oPolicy = result?.oPolicy;

            if (oPolicy == null)
            {
                await LogError("HandleMedicalPolicyAsync",
                    new InvalidOperationException(
                        "No medical policy was produced — order NOT finalised."), siteId);

                throw pipelineError
                    ?? new InvalidOperationException("ProcessMedicalFlow returned a null policy.");
            }

            // ── Policy EXISTS — every step below is individually guarded so no
            // single failure can stop the ones after it. ──
            await LogInfo("Policy.Finalise",
                $"medical policy {oPolicy.PolicyID} created for order {order.Id} — finalising", siteId);

            await RunSafe("RegisterAndMapUser", siteId,
                TryRegisterAndMapUserAsync(ctx, order, siteId, customerId));

            await RunSafe("MapOrderReference", siteId,
                MapOrderReference(oPolicy.SegmentCode, order));

            await RunSafe("SavePolicyId", siteId, SavePolicyId(new GeneralPolicy
            {
                DocumentNo  = oPolicy.PolicyID,
                NetPremium  = oPolicy.TotalPremium,
                SegmentCode = oPolicy.SegmentCode
            }, order));

            await RunSafe("SendReceipt", siteId, SendReceipt(order.Id));

            await LogInfo("Policy.Finalise", $"order {order.Id} finalised", siteId);
        }




        public async Task CalculateQoutation( long userId,long CartId,List<IdFiles> files)
        {
            var siteId = await _workContext.GetCurrentSiteIdAsync()
          .WaitAsync(TimeSpan.FromSeconds(10));
            var cart = await GetCartDetails(siteId,userId,CartId);

			var cartObj = cart?.Items?.FirstOrDefault();
			if (cartObj == null)
				throw new Exception("Order cart item not found");

			var ctx = BuildOrderCartContext(cartObj.AdditionalDetails);

			if (string.IsNullOrWhiteSpace(ctx.ProductCode))
				throw new Exception("ProductCode is missing in AdditionalDetails");
			if (string.IsNullOrWhiteSpace(ctx.NationalId))
				throw new Exception("NationalId is missing in AdditionalDetails");
			if (string.IsNullOrWhiteSpace(ctx.DocumentId))
				throw new Exception("DocumentId/PolicyID is missing in AdditionalDetails");

			// ── Customer: get or create ──
			var customerId = await _financialApi.GetCustomerByNationalId(ctx.NationalId);
			if (customerId == 0)
				customerId = await _financialApi.InsertCustomer(BuildInsertCustomerPayload(ctx.Root, ctx.Email));
			else
				await _financialApi.UpdateCustomer(BuildUpdateCustomerPayload(ctx.Root, customerId, ctx.Email));



            try
            {
				await WithStepTimeout("DeleteAttachment",
			_generalApi.DeleteAttachment(ctx.DocumentId));




				await WithStepTimeout("InsertAttachments",
		_generalApi.InsertAttachments(files, ctx.DocumentId));


			}
			catch
            {

            }

		
			await WithStepTimeout("InsertQuotationInterest",
				_generalApi.InsertQuotationInterest(ctx.DocumentId, customerId, ctx.Root, ctx.GeneralRequest));


			await WithStepTimeout("InsertQuotationCovers",
				_generalApi.InsertQuotationCovers(ctx.DocumentId));

			await WithStepTimeout("CalculateQuotation",
				_generalApi.CalculateQuotation(ctx.DocumentId));
            try
            {
                  await WithStepTimeout("CheckApproval",
                _generalApi.CheckApproval(ctx.DocumentId));
            }
            catch (Exception)
            {

            }
          

        }


		private async Task<ConvertToPolicyResponse> ProcessTravelAndPAFlow(
            long customerId, JObject details, string documentId, JObject generalRequest, long siteId)
        {
            await LogInfo("Policy.Pipeline", $"start document {documentId}", siteId);

            await RunCritical("UpdateQuotation", siteId,
                _generalApi.UpdateQuotation(customerId, details, documentId, generalRequest));

            await RunCritical("CalculateQuotation", siteId,
                _generalApi.CalculateQuotation(documentId));
            /*
          await WithStepTimeout("InsertAttachments",
              _generalApi.InsertAttachments(details, documentId, generalRequest));

           
         await WithStepTimeout("InsertQuotationInterest",
             _generalApi.InsertQuotationInterest(documentId, customerId, details, generalRequest));

         await WithStepTimeout("InsertQuotationCovers",
             _generalApi.InsertQuotationCovers(documentId));

         await WithStepTimeout("CalculateQuotation",
             _generalApi.CalculateQuotation(documentId));
         */
            /* create Custome Api For FIC Bussines For Cart*/

            /* Events Will Send To Me ConnectiionId And Return AdditionDetails to the event */

            // NON-CRITICAL by design: a rejected approval check must not stop the
            // journey. It is logged rather than swallowed — previously a successful
            // (slow) CheckApproval was where the whole journey silently died.
        

            await RunCritical("UpdateQuotationStatus", siteId,
                _generalApi.UpdateQuotationStatus(documentId));

            // This is the ONLY call that creates the policy, i.e. the one point where
            // money and policy can diverge: if it succeeds remotely but we abandon the
            // await, the customer is charged for a policy we never record and there is
            // no lookup endpoint to recover it. It therefore gets the full client
            // window rather than the 55 s step ceiling. Safe now that the journey runs
            // detached from the webhook request.
            ConvertToPolicyResponse result;
            try
            {
                result = await RunCritical("ConvertToPolicy", siteId,
                    _generalApi.ConvertToPolicy(documentId), TimeSpan.FromMinutes(2));

				await RunSafe("CheckApproval", siteId, _generalApi.CheckApproval(result?.GeneralPolicy?.DocumentID.ToString()));

			}
            catch (TimeoutException ex)
            {
                await LogError("Policy.ConvertToPolicy.NEEDS_RECONCILIATION",
                    new TimeoutException(
                        $"ConvertToPolicy timed out for document {documentId}. " +
                        "A policy MAY exist remotely — verify before retrying, a retry would duplicate it.", ex),
                    siteId);
                throw;
            }

            await LogInfo("Policy.Pipeline",
                $"document {documentId} converted to policy {result?.GeneralPolicy?.DocumentID}", siteId);

            return result;
        }

        private async Task<ConvertQuotationARTResponseVm> ProcessMedicalFlow(
            long customerId, JObject details, string documentId, JObject generalRequest,
            long siteId, OrderCartContext ctx)
        {
            await LogInfo("Policy.Pipeline", $"medical start document {documentId}", siteId);

            // CRITICAL — must succeed before the policy can be created
            await RunCritical("UpdatePolicy", siteId,
                _medicalApi.UpdatePolicy(customerId, details, documentId, generalRequest));

            var result = await RunCritical("ConvertQuotationART", siteId,
                _medicalApi.ConvertQuotationART(documentId));

            if (result?.oPolicy == null)
                throw new InvalidOperationException("ConvertQuotationART returned null policy.");

            await LogInfo("Policy.Pipeline",
                $"medical policy {result.oPolicy.PolicyID} created", siteId);

            // ── The policy now EXISTS. Nothing below may throw, or the caller
            // would treat a created policy as a failed one. ──
            try
            {
                var postResult = await _medicalApi.PostPolicy(result.oPolicy.PolicyID.ToString())
                    .WaitAsync(TimeSpan.FromMinutes(1));

                var json = JObject.Parse(postResult.ToString());
                var transactions = json["colINS_TRANSACTIONS_DOL"] as JArray;
                long? firstId = transactions?.FirstOrDefault()?["ID"]?.Value<long>();

                var quotationDate = ctx.Root.GetDateSafe("quotationDate") ?? DateTime.Now;
                await RunSafe("GenerateReceipt", siteId, _financialApi.GenerateReceipt(
                    result.oPolicy?.PolicyNo.ToString(),
                    firstId,
                    result.oPolicy?.TotalPremium.ToString(),
                    quotationDate.ToString("dd/MM/yyyy")));
            }
            catch (Exception ex)
            {
                await LogError("Policy.PostPolicy", ex, siteId);
            }

            return result;
        }

        private static async Task WithStepTimeout(string stepName, Task stepTask)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(55));
            try
            {
                await stepTask.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"Pipeline step '{stepName}' timed out after 55 seconds.");
            }
        }

        // Generic overload for steps that return a value (e.g. customer lookup/insert).
        private static async Task<T> WithStepTimeout<T>(string stepName, Task<T> stepTask)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(55));
            try
            {
                return await stepTask.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"Pipeline step '{stepName}' timed out after 55 seconds.");
            }
        }



        private async Task TryRegisterAndMapUserAsync(
            OrderCartContext ctx, Order order, long siteId, long customerId)
        {
            // NEVER throws — registration failure must not block order finalisation.
            try
            {
                using var checkCts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                User existing;
                try
                {
                    existing = await userRepository.Query()
                        .AsNoTracking()
                        .Where(x => x.NormalizedUserName == ctx.NationalId.ToUpper())
                        .FirstOrDefaultAsync(checkCts.Token);
                }
                catch { return; }

                if (existing != null)
                {
                    var UserOptions = new List<UserOptionsVM>();

                    UserOptions.Add(new UserOptionsVM
                    {
                        PropertyName = "CustomerId",
                        Value = customerId.ToString(),
                        IsEditable = false,
                        IsVisible = true,
                        ControlType = EControlTypes.Text
                    });

                    UserOptions.Add(new UserOptionsVM
                    {
                        PropertyName = "NationalId",
                        Value = ctx.NationalId.ToUpper(),
                        IsEditable = false,
                        IsVisible = true,
                        ControlType = EControlTypes.Text
                    });


                    await userService.AddUpdateUserOptions(ctx.NationalId.ToUpper(), UserOptions);


                }
                else
                {

                    var userInfo = new CustomerInfoInsuranceViewModel
                    {
                        Phone = ctx.Phone,
                        NationalId = ctx.NationalId,
                        Name = ctx.Name,
                        Email = ctx.Email ?? "customer@customer.com",
                    };

                    RegisterationResultDTO result;
                    try
                    {
                        result = await Registration(ctx, siteId, customerId, userInfo)
                            .WaitAsync(TimeSpan.FromMinutes(1));
                    }
                    catch (TimeoutException)
                    {
                        await LogError("TryRegisterAndMapUserAsync",
                            new TimeoutException("Registration timed out after 30 seconds"), siteId);
                        return;
                    }
                    catch (Exception ex)
                    {
                        await LogError("TryRegisterAndMapUserAsync", ex, siteId);
                        return;
                    }

                    if (result.UserId != 0)
                        await MappingUserIdToOrder(result.UserId, order);
                }
            }
            catch (Exception ex)
            {
                await LogError("TryRegisterAndMapUserAsync.Outer", ex, siteId);
            }
        }

        private async Task<RegisterationResultDTO> Registration(
            OrderCartContext ctx,
            long siteId,
            long customerId,
            CustomerInfoInsuranceViewModel userInfo)
        {
            try
            {
                var userName = string.IsNullOrWhiteSpace(userInfo.NationalId)
                    ? userInfo.Email?.Trim()
                    : userInfo.NationalId.Trim();

                User existingUser;
                try { existingUser = await userManager.FindByNameAsync(userName).WaitAsync(TimeSpan.FromMinutes(1)); }
                catch { existingUser = null; }

                if (existingUser != null)
                    return new RegisterationResultDTO { UserId = existingUser.Id, Message = "User already exists" };

                if (!string.IsNullOrWhiteSpace(userInfo.Email))
                {
                    User existingByEmail;
                    try { existingByEmail = await userManager.FindByEmailAsync(userInfo.Email.Trim()).WaitAsync(TimeSpan.FromMinutes(1)); }
                    catch { existingByEmail = null; }

                    if (existingByEmail != null)
                        return new RegisterationResultDTO { UserId = existingByEmail.Id, Message = "User already exists by email" };
                }

                DateTimeOffset? birthDate = !string.IsNullOrWhiteSpace(ctx?.BirthDate)
                    && DateTimeOffset.TryParse(ctx.BirthDate, out var parsed)
                    ? parsed : null;

                var user = new User
                {
                    UserName     = userName,
                    Email        = userInfo.Email,
                    FullName     = userInfo.Name?.Trim(),
                    PhoneNumber  = userInfo.Phone?.TrimStart('+')?.Trim(),
                    CreatedById  = 1,
                    CreationDate = DateTimeOffset.UtcNow,
                    Status       = EStatus.Active,
                    StatusDate   = DateTimeOffset.UtcNow,
                    Type         = EUsersTypes.Customer,
                    DateOfBirth  = birthDate.HasValue
                        ? new DateTimeOffset(birthDate.Value.DateTime, TimeSpan.Zero)
                        : null
                };

                user.UserSites.Add(new UsersSites { User = user, SiteId = siteId, IsDefault = true });

                using var groupCts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                CustomerGroup customerGroup;
                try { customerGroup = await customerGrouprepository.Query().FirstOrDefaultAsync(x => x.Id == 1, groupCts.Token); }
                catch { customerGroup = null; }

                if (customerGroup != null)
                {
                    user.CustomerGroups.Add(new CustomerGroupUser
                    {
                        CustomerGroupId = customerGroup.Id,
                        User            = user,
                        CreationDate    = DateTimeOffset.UtcNow
                    });
                    user.TwoFactorEnabled = customerGroup.EnableTwoFactorAuthentication;
                }

                user.UserRoles.Add(new UserRole { User = user, RoleId = (long)ERoleTypes.Customer });

                user.UserOptions.Add(new Entities.Entity.Core.UserOptions
                {
                    PropertyName = "NationalId",
                    Value        = userInfo.NationalId,
                    IsEditable   = false,
                    IsVisible    = true,
                    ControlType  = EControlTypes.Text
                });
                user.UserOptions.Add(new Entities.Entity.Core.UserOptions
                {
                    PropertyName = "CustomerId",
                    Value        = customerId.ToString(),
                    IsEditable   = false,
                    IsVisible    = true,
                    ControlType  = EControlTypes.Text
                });

                // UserManager.CreateAsync has no CancellationToken — outer WaitAsync(30s) is the guard
                var result = await userManager.CreateAsync(user, "Eska@1234");

                if (result.Succeeded)
                    return new RegisterationResultDTO { UserId = user.Id, Message = "User created successfully" };

                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new RegisterationResultDTO { UserId = 0, Message = $"Failed: {errors}" };
            }
            catch (Exception ex)
            {
                return new RegisterationResultDTO { UserId = 0, Message = ex.Message };
            }
        }

        private async Task MapOrderReference(string orderReferenceId, Order order)
        {
            try
            {
                order.OrderReferanceId = orderReferenceId;
                await orderRepository.SaveChangesAsync()
                    .WaitAsync(TimeSpan.FromMinutes(1));
            }
            catch (Exception ex)
            {
                await dcmsLogService.AddError("MapOrderReference", ex.Message, ex.StackTrace, null, 0);
            }
        }

        private async Task MappingUserIdToOrder(long userId, Order order)
        {
            try
            {
                var cart = order.Cart;
                order.CustomerId        = userId;
                order.CreatedById       = userId;
                order.LatestUpdatedById = userId;
                cart.CustomerId         = userId;
                cart.CreatedById        = userId;
                await orderRepository.SaveChangesAsync()
                    .WaitAsync(TimeSpan.FromSeconds(15));
            }
            catch (Exception ex)
            {
                await dcmsLogService.AddError("MappingUserIdToOrder", ex.Message, ex.StackTrace, null, 0);
            }
        }

        public async Task SavePolicyId(GeneralPolicy policy, Order order)
        {
            try
            {
                if (order == null) return;
                var cartObj = order.Cart?.Items?.FirstOrDefault();
                var OrderObj = order.OrderItems?.FirstOrDefault();
                if (cartObj == null) return;

                var cartItem = JsonConvert.DeserializeObject<JObject>(
                    cartObj.AdditionalDetails) ?? new JObject();

                cartItem["PolicyId"]   = policy.DocumentID;

                cartItem["PolicyID"] = policy.DocumentID;
				cartItem["DocumentNumber"] = policy.DocumentNo;

				cartItem["PolicyCode"] = policy.SegmentCode;
                cartObj.AdditionalDetails = cartItem.ToString(Newtonsoft.Json.Formatting.None);
                OrderObj.AdditionalDetails= cartObj.AdditionalDetails;
                await _cartContext.SaveChangesAsync()
                    .WaitAsync(TimeSpan.FromMinutes(1));
                await orderRepository.SaveChangesAsync()
                  .WaitAsync(TimeSpan.FromMinutes(1));
            }
            catch (Exception ex)
            {
                await dcmsLogService.AddError("SavePolicyId", ex.Message, ex.StackTrace, null, 0);
            }
        }

        public async Task<Order> GetOrderDetails(long orderId) =>
            await _orderService.GetOrderById(orderId)
                .WaitAsync(TimeSpan.FromMinutes(1));

        public async Task<Cart> GetCartDetails(long siteId,long userId,long CartId) {

            return await _ICartService.GetActiveCart(userId , CartTypes.Shop , siteId ,CartId);
				 
		} 


		private OrderCartContext BuildOrderCartContext(string additionalDetails)
        {
            if (string.IsNullOrWhiteSpace(additionalDetails))
                throw new Exception("AdditionalDetails is empty");

            var root = JObject.Parse(additionalDetails);

            var productCode = root.GetStringSafe(
                "productCode", "personalDetails.productCode") ?? string.Empty;

            var quotationResponse = string.Equals(productCode, "Medical", StringComparison.OrdinalIgnoreCase) ? (root.GetObjectSafe("insertPolicyRes") == null ? root.GetObjectSafe("quotationResponse") : root.GetObjectSafe("insertPolicyRes")) : (root.GetObjectSafe("quotationResponse") ?? new JObject());

            var nationalId = root.GetStringSafe("NationalId", "NationalID", "nationalId", "nationaL_ID",
                                   "personalDetails.nationalId", "personalDetails.NationalId", "personalDetails.NationalID");
            var phone = root.GetStringSafe("phone", "personalDetails.phone", "contactInfo.phone");
            var interestName = root.GetStringSafe("InterestName", "OwnerName", "InsuredName", "personalDetails.fullName");
            var birthDate = root.GetStringSafe("BirthDate", "dateOfBirth", "personalDetails.dateOfBirth");
            var quotationDate = root.GetStringSafe("quotationDate");
            var email = root.GetStringSafe("Email", "email", "personalDetails.email", "contactInfo.email");
			var documentId = root.GetStringSafe("PolicyID");


			root["NationalId"]   = nationalId;
            root["GaNationalId"] = nationalId;
            root["phone"]        = phone;
            root["InterestName"] = interestName;
            root["BirthDate"]    = birthDate;

            if (!string.IsNullOrWhiteSpace(quotationDate))
                root["quotationDate"] = quotationDate;

            var idFiles = root.GetArraySafe("idFiles", "personalDetails.idFiles");
            if (idFiles != null)
                root["idFiles"] = idFiles.DeepClone();

            JObject generalRequest;
        
                var generalQuotation = string.Equals(productCode, "Medical", StringComparison.OrdinalIgnoreCase) ? quotationResponse : quotationResponse.GetObjectSafe("GeneralQuotation");
                generalRequest = generalQuotation ?? (JObject)quotationResponse.DeepClone();
            

            generalRequest.SetIfMissing("ClassID",
                root.GetTokenSafe("ClassID", "selectedPlan.quickPriceRequest.ClassID"));
            generalRequest.SetIfMissing("PolicyType",
                root.GetTokenSafe("PolicyType", "selectedPlan.quickPriceRequest.PolicyTypeID"));
            generalRequest.SetIfMissing("PolicyDays", root.GetTokenSafe("PolicyDays"));
            generalRequest.SetIfMissing("QuotationValidity", root.GetTokenSafe("QuotationValidity"));
            generalRequest.SetIfMissing("SumInsured",
                root.GetTokenSafe("SumInsured", "selectedPlan.coverage"));
            generalRequest.SetIfMissing("BirthDate",
                root.GetTokenSafe("BirthDate", "personalDetails.dateOfBirth"));
            generalRequest.SetIfMissing("ExpiryDate", root.GetTokenSafe("ExpiryDate"));
            generalRequest.SetIfMissing("Address", root.GetTokenSafe("InsuredAddress"));
            generalRequest.SetIfMissing("InterestType",
                root.GetTokenSafe("InterestType", "tripDetails.InterestType"));

            root.SetIfMissing("EffectiveDate", generalRequest.GetTokenSafe("EffectiveDate"));
            root.SetIfMissing("ExpiryDate", generalRequest.GetTokenSafe("ExpiryDate"));
            var x = JsonConvert.SerializeObject(generalRequest);
             documentId =
                documentId==null?
                            string.Equals(productCode, "Medical", StringComparison.OrdinalIgnoreCase)
                            ? generalRequest.GetStringSafe("PolicyID", "DocumentID", "documentId", "GeneralQuotation.PolicyID")
                            : generalRequest.GetStringSafe("DocumentID", "documentId")
               :documentId;

            return new OrderCartContext
            {
                Root              = root,
                QuotationResponse = quotationResponse,
                GeneralRequest    = generalRequest,
                ProductCode       = productCode,
                NationalId        = nationalId,
                DocumentId        = documentId,
                BirthDate         = birthDate,
                Email             = email,
                Phone             = phone,
                Name              = interestName
            };
        }


        

        private InsertCustomerRequest BuildInsertCustomerPayload(JObject cart, string Email) =>
            new InsertCustomerRequest
            {
                CompanyId           = _generalApi.Settings.CompanyId,
                CompanyId2           = _generalApi.Settings.CompanyId,
                BranchId            = _generalApi.Settings.BranchId,
                Email            = Email,
                NationalId          = cart.GetStringSafe("NationalId", "GaNationalId"),
                Name                = cart.GetStringSafe("InterestName", "InsuredName", "personalDetails.fullName"),
                CountryCode         = cart.GetStringSafe("personalDetails.nationalityCode") ?? "JO",
                CreatedBy           = _generalApi.Settings.DefaultUser,
                BlackList = 0,
                CityCode = cart.GetIntSafe(1, "personalDetails.city") ,
                CST_SUP_FLAG = 3,
				CUSTOMERTYPE=1,

				Gender = 1,
                IndividualOrCompany = 1,
                IsDelete = 0,
                TAXABLE=1,
                MainAddress= cart.GetStringSafe("quotationResponse.GeneralQuotation.InsuredAddress", "InsuredAddress", "InsuredAddress", "personalDetails.InsuredAddress"),
                Language = 1,
                LocalOrForeign = 1,
                Nationality         = "JO",
                OnHold = 0,
                Phone               = cart.GetStringSafe("phone", "personalDetails.phone"),
                StartDate           = DateTime.Now.ToString("MM-dd-yyyy"),
                CreationDate        = DateTime.Now.ToString("MM-dd-yyyy"),
                Title               = 18,
            };

        private UpdateCustomerRequest BuildUpdateCustomerPayload(JObject cart, long customerId,string Email) =>
           new UpdateCustomerRequest
           {
               ID                  = Convert.ToInt32(customerId),

               CompanyId = _generalApi.Settings.CompanyId,          // CRG_COM_ID
               CRG_COM_ID2 = _generalApi.Settings.CompanyId,         // CRG_COM_ID2
               CompanyId2 = _generalApi.Settings.CompanyId,         // CRG_COM_ID2
               BranchId = _generalApi.Settings.BranchId,            // CRG_BRN_ID
               CityCode = cart.GetIntSafe(1, "personalDetails.city"),                                        // CRG_CTY_CODE
               Email=Email,
               Name = cart.GetStringSafe(
        "InterestName",
        "InsuredName",
        "personalDetails.fullName"),

               CreatedBy = _generalApi.Settings.DefaultUser,
               Phone = cart.GetStringSafe("phone", "personalDetails.phone"),

               //SEGMENT_CODE = "DEFAULT", // Replace with the correct segment code
               CountryCode = cart.GetStringSafe("personalDetails.nationalityCode") ?? "JO",
			   CUSTOMERTYPE = 1,
			   Language = 1,
               BlackList = 0,
               OnHold = 0,
               CST_SUP_FLAG = 3,
               COMP_INDV_FLAG = 0,
               TAXABLE = 1,
               IsDelete = 0,

               StartDate = DateTime.Now.ToString("MM-dd-yyyy"),
               CreationDate = DateTime.Now.ToString("MM-dd-yyyy"),

               NationalId = cart.GetStringSafe("GaNationalId", "NationalId"),
              
               Nationality = "JO",
               LocalOrForeign = 1,
               IndividualOrCompany = 1,
               Gender = 1,
               Title = 18,

               MainAddress = cart.GetStringSafe(
        "quotationResponse.GeneralQuotation.InsuredAddress",
        "InsuredAddress",
        "personalDetails.InsuredAddress")
           };



        public async Task SendReceipt(long orderId)
        {
            try
            {
                long siteId = await _workContext.GetCurrentSiteIdAsync()
                    .WaitAsync(TimeSpan.FromSeconds(10));
                Order order = await _orderService.GetOrderById(orderId)
                    .WaitAsync(TimeSpan.FromSeconds(15));
                var CultureId = _workContext.GetCurrentCultureId();
                var Culture = siteCultureService.GetCultureById(CultureId);
                var settings = await siteSettings.GetSiteSettingsByModule("IntegrationTemplates", siteId)
                    .WaitAsync(TimeSpan.FromSeconds(10));

                var (adminReceiptEmailTemplateId, smsReceiptTemplateId, notificationType) =
                    GetSettingsValues(settings, Culture.Culture);

                var AdminEmails = GetAdminEmails(settings);
                var Parameters = new List<EmailParamsVM>();

                Parameters.Add(new EmailParamsVM { Param = "[SubtotalString]", Value = currencyService.FormatByUserCurrency(order.SubTotal) });
                Parameters.Add(new EmailParamsVM { Param = "[name]", Value = order.Customer.FullName });
                Parameters.Add(new EmailParamsVM
                {
                    Param = "[phonenumber]",
                    Value = order.ShippingAddressId.HasValue
                        ? (string.IsNullOrEmpty(order.ShippingAddress.Phone) ? order.Customer.PhoneNumber : order.ShippingAddress.Phone)
                        : order.Customer.PhoneNumber
                });

                var allKeys = await localizationServices.GetLocalization(string.Empty, siteId, CultureId)
                    .WaitAsync(TimeSpan.FromMinutes(1));

                Parameters.Add(new EmailParamsVM { Param = "[DiscountAmount]", Value = currencyService.FormatByUserCurrency(order.DiscountAmount) });
                Parameters.Add(new EmailParamsVM { Param = "[OrderId]", Value = order.Id.ToString() });
                Parameters.Add(new EmailParamsVM
                {
                    Param = "[OrderEncryptedId]",
                    Value = HttpUtility.UrlEncode(EncryptDecryptHelper.Encrypt(orderId.ToString()))
                });
                Parameters.Add(new EmailParamsVM
                {
                    Param = "[Date]",
                    Value = order.CreatedOn.ToLocalTime().ToString("MM/dd/yyyy hh:mm tt")
                });
                Parameters.Add(new EmailParamsVM
                {
                    Param = "[ProductName]",
                    Value = string.Join(',', order.OrderItems
                        .SelectMany(x => x.Product.ProductDetails)
                        .Where(x => x.CultureId == CultureId)
                        .Select(x => x.Name))
                });

                var PaymentMethod = allKeys
                    .Where(x => x.Key == "SharedResources").Select(x => x.Value).FirstOrDefault()
                    ?.Where(x => x.Key == order.PaymentMethod.ToString()).Select(x => x.Value).FirstOrDefault();

                Parameters.Add(new EmailParamsVM
                {
                    Param = "[PaymentMethod]",
                    Value = string.IsNullOrEmpty(PaymentMethod) ? order.PaymentMethod.ToString() : PaymentMethod
                });
                Parameters.Add(new EmailParamsVM { Param = "[OrderCurrencyCode]", Value = order.CurrencyCode.ToString() });

                var AdditionalDetails = order.Cart.Items
                    .Where(x => !string.IsNullOrEmpty(x.AdditionalDetails))
                    .Select(x => x.AdditionalDetails)
                    .FirstOrDefault();

                if (AdditionalDetails != null)
                    FillPolicyDatesAndIdsIntoParameters(Parameters, AdditionalDetails);

                Parameters.Add(new EmailParamsVM { Param = "[OrderTotal]", Value = currencyService.FormatByUserCurrency(order.OrderTotal) });
                Parameters.Add(new EmailParamsVM { Param = "[CouponCode]", Value = order.CouponCode });

                var phoneNumber = !string.IsNullOrEmpty(order.Customer?.PhoneNumber)
                    ? order.Customer?.PhoneNumber
                    : order.ShippingAddress?.Phone;

                var notificationObject = new NotificationObject
                {
                    MessageBody      = string.Empty,
                    MessageTitle     = "Order Receipt",
                    SendVia          = SendBy.MobileNumber,
                    SendValue        = new List<string> { phoneNumber },
                    NotificationType = NotificationType.SMS,
                    EmailOptions     = new EmailSenderVM
                    {
                        Emails     = new List<string>(),
                        TemplateId = smsReceiptTemplateId,
                        Params     = Parameters
                    },
                    EmailTemplateId  = adminReceiptEmailTemplateId,
                };

                try
                {
                    if (!string.IsNullOrEmpty(phoneNumber) &&
                        (notificationType == NotificationType.SMS || notificationType == NotificationType.BothSMSEmail))
                    {
                        await testNotifier.SendMessageAsync(NotificationType.SMS, null, notificationObject, siteId)
                            .WaitAsync(TimeSpan.FromMinutes(1));
                    }

                    notificationObject.SendVia          = SendBy.Email;
                    notificationObject.NotificationType = NotificationType.Email;

                    if (AdminEmails.Count > 0 &&
                        (notificationType == NotificationType.BothSMSEmail || notificationType == NotificationType.Email))
                    {
                        notificationObject.SendValue               = AdminEmails;
                        notificationObject.EmailOptions.TemplateId = adminReceiptEmailTemplateId;
                        await testNotifier.SendMessageAsync(NotificationType.Email, null, notificationObject, siteId)
                            .WaitAsync(TimeSpan.FromMinutes(1));
                    }

                    if( !string.IsNullOrEmpty(order.Customer.Email) && (notificationType == NotificationType.BothSMSEmail || notificationType == NotificationType.Email))
                    {
						notificationObject.SendValue = [order.Customer.Email];
						notificationObject.EmailOptions.TemplateId = adminReceiptEmailTemplateId;
						await testNotifier.SendMessageAsync(NotificationType.Email, null, notificationObject, siteId)
							.WaitAsync(TimeSpan.FromMinutes(1));
					}
                }
                catch (Exception ex)
                {
                    await dcmsLogService.AddError(
                        "SendReceipt Form Emails",
                        ex.InnerException?.Message ?? ex.Message,
                        ex.Message, null, siteId);
                }
            }
            catch (Exception ex)
            {
                await LogError("SendReceipt", ex, siteId: 0);
            }
        }   

        
        private void FillPolicyDatesAndIdsIntoParameters(List<EmailParamsVM> parameters, string additionalDetails)
        {
            if (string.IsNullOrWhiteSpace(additionalDetails)) return;
            try
            {
                var ctx = BuildOrderCartContext(additionalDetails);
                var effectiveDate = ctx.Root.GetDateSafe("EffectiveDate") ?? ctx.GeneralRequest.GetDateSafe("EffectiveDate");
                var expiryDate = ctx.Root.GetDateSafe("ExpiryDate")    ?? ctx.GeneralRequest.GetDateSafe("ExpiryDate");

                parameters.Add(new EmailParamsVM { Param = "[EffectiveDate]", Value = effectiveDate?.ToString("yyyy-MM-dd") });
                parameters.Add(new EmailParamsVM { Param = "[ExpiryDate]", Value = expiryDate?.ToString("yyyy-MM-dd") });
                parameters.Add(new EmailParamsVM { Param = "[PolicyId]", Value = ctx.Root.GetStringSafe("PolicyId") });
                parameters.Add(new EmailParamsVM { Param = "[PolicyCode]", Value = ctx.Root.GetStringSafe("PolicyCode") });
            }
            catch { }
        }

        private (string, string, NotificationType) GetSettingsValues(List<SiteSettingsVM> settings, string Culture)
        {
            if (settings.Count > 0)
            {
                var adminId = Culture.Contains("en")
                    ? GetStringSettingValue(settings, "IntegrationEmailTemplateId")
                    : GetStringSettingValue(settings, "IntegrationEmailTemplateIdArabic");

                var smsId = Culture.Contains("en")
                    ? GetStringSettingValue(settings, "IntegrationSMSReceiptTemplateId")
                    : GetStringSettingValue(settings, "IntegrationSMSReceiptTemplateIdArabic");

                var notificationType = settings.FirstOrDefault(x => x.Id == "NotificationType")?.Value != null
                    ? (NotificationType)Enum.Parse(typeof(NotificationType),
                        settings.First(x => x.Id == "NotificationType").Value, true)
                    : NotificationType.BothSMSEmail;

                return (adminId, smsId, notificationType);
            }
            return (string.Empty, string.Empty, NotificationType.BothSMSEmail);
        }

        private List<string> GetAdminEmails(List<SiteSettingsVM> settings)
        {
            if (settings.Count > 0)
            {
                var s = settings.FirstOrDefault(x => x.Id == "IntegrationAdminReceiptEmails");
                return s != null ? s.Value.Split(",").ToList() : new List<string>();
            }
            return new List<string>();
        }

        private string GetStringSettingValue(List<SiteSettingsVM> settings, string settingId)
        {
            var s = settings.FirstOrDefault(x => x.Id == settingId);
            return s?.Value ?? string.Empty;
        }

        private async Task LogError(string source, Exception ex, long siteId = 0)
        {
            try
            {
                await dcmsLogService.AddError(
                    source,
                    ex.InnerException?.Message ?? ex.Message,
                    ex.StackTrace, null, siteId);
            }
            catch { }
        }

        // ── Step instrumentation ──────────────────────────────────────────────
        // Every integration step runs through one of the Run* helpers below so
        // that start / outcome / elapsed time is always recorded. Without this
        // a step that silently stalls leaves no trace at all — which is exactly
        // how the PayFort journey was dying between CheckApproval and
        // ConvertToPolicy with nothing in the logs.

        private async Task LogInfo(string source, string message, long siteId = 0)
        {
            try { await dcmsLogService.AddInfo(source, message, null, siteId); }
            catch { }
        }

        /// <summary>
        /// Runs a step that MUST succeed. Logs start/ok/fail, then rethrows so the
        /// caller can abort the journey.
        /// </summary>
        private async Task<T> RunCritical<T>(string step, long siteId, Task<T> task, TimeSpan? timeout = null)
        {
            var limit = timeout ?? StepTimeout;
            var sw = Stopwatch.StartNew();
            await LogInfo($"Policy.{step}", "start", siteId);
            try
            {
                var result = await task.WaitAsync(limit);
                await LogInfo($"Policy.{step}", $"ok ({sw.ElapsedMilliseconds} ms)", siteId);
                return result;
            }
            catch (TimeoutException)
            {
                var timeoutEx = new TimeoutException(
                    $"Step '{step}' timed out after {limit.TotalSeconds:0} s.");
                await LogError($"Policy.{step}", timeoutEx, siteId);
                throw timeoutEx;
            }
            catch (Exception ex)
            {
                await LogError($"Policy.{step}", ex, siteId);
                throw;
            }
        }

        private async Task RunCritical(string step, long siteId, Task task)
        {
            await RunCritical<bool>(step, siteId, Wrap(task));
        }

        /// <summary>
        /// Runs a step that must NEVER break the chain. Logs the failure and
        /// returns false — the caller keeps going regardless.
        /// </summary>
        private async Task<bool> RunSafe(string step, long siteId, Task task)
        {
            var sw = Stopwatch.StartNew();
            await LogInfo($"Policy.{step}", "start", siteId);
            try
            {
                await task.WaitAsync(StepTimeout);
                await LogInfo($"Policy.{step}", $"ok ({sw.ElapsedMilliseconds} ms)", siteId);
                return true;
            }
            catch (Exception ex)
            {
                await LogError($"Policy.{step}", ex, siteId);
                return false;
            }
        }

        private static readonly TimeSpan StepTimeout = TimeSpan.FromSeconds(55);

        // Adapts a non-generic Task so the generic Run* implementation can be reused.
        private static async Task<bool> Wrap(Task task)
        {
            await task;
            return true;
        }
    }
}