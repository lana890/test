using EskaCMS.Core.Data;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.EmailSender.SMTP.Services;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Extensions;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Notifiers;
using EskaCMS.Entities.Entity;

using EskaCommerce.Orders.Services;
using EskaCommerce.ShippingJeebly.Services;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels;
using EskaCommerce.ShoppingCart.Services;
using EskaCommerce.TelrPayment.Models;
using EskaCommerce.TelrPayment.Services.Interfaces;
using EskaCommerce.UserCatalog.Dtos;
using EskaCommerce.UserCatalog.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EskaCMS.Entities.Entity;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using EskaCMS.Entities.Entity.Payments;
using EskaCMS.Entities.Entity.UserCatalog;
using EskaCMS.Entities.Entity.TelrPayment;
using EskaCMS.Entities.Entity.Orders;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Enum;
using static EskaCMS.Entities.Enum.PlanEnums;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure;
using EskaCommerce.UserCatalog.Services;
using EskaCommerce.Payments.Helper;
using EskaCMS.DataAccess.Data;
using EskaCommerce.Shipments.Services.ShippingJeeblyService.Interfaces;

namespace EskaCommerce.TelrPayment.Services
{
    public class TerlPaymentService : ITelrPayment
    {

        private readonly string _assembly;
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly IWorkContext _workContext;
        private readonly IRepository<Payment> _paymentRepository;
        private readonly ICurrencyService _currencyService;
        private readonly IRepositoryWithTypedId<PaymentProvider, string> _paymentProviderRepository;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IEmailSender _emailSender;
        private readonly IUserPlansService _UserPlansService;
        private readonly IUserBookingService _UserBookingService;
        private readonly IRepository<TransactionHistory> _transHistoryRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly ISiteSettingsService _settingservice;
        private readonly IdcmsLogService _dcmsLogService;
        private readonly EskaDCMSDbContext _context;
        private readonly IJeeblyService _JeeblyService;
        private readonly IRepository<PaymentRefund> _paymentrefundRepository;
        private readonly IUserPlansService _planServices;
        private readonly IRepository<UserPlans> _userplanRepository;
        private readonly IRepository<UserPlansHistory> _userplanHistoryRepository;
        private readonly ITestNotifier _testNotifier;
        private readonly ISubscriptionServices SubscriptionServices;
        private readonly IHttpClientFactory _httpClientFactory;

        public TerlPaymentService(
          ICartService cartService,
          IOrderService orderService,
          IWorkContext workContext,
          IRepository<Payment> paymentRepository,
          ICurrencyService currencyService,
          IRepositoryWithTypedId<PaymentProvider, string> paymentProviderRepository,
          IEmailTemplateService emailTemplateService,
          IEmailSender emailSender,
          IUserPlansService UserPlansService,
          IRepository<TransactionHistory> transHistoryRepository,
          IdcmsLogService dcmsLogService,
          ISiteSettingsService settingservice,
          EskaDCMSDbContext context,
          IJeeblyService JeeblyService,
          IRepository<Order> orderRepository,
          IRepository<PaymentRefund> paymentrefundRepository,
          IUserPlansService planServices,
          IRepository<UserPlans> userplanRepository,
            IRepository<UserPlansHistory> userplanHistoryRepository,
            IUserBookingService UserBookingService,
            ITestNotifier testNotifier,
            ISubscriptionServices SubscriptionServices,
            IHttpClientFactory httpClientFactory

          )
        {
            _cartService = cartService;
            _orderService = orderService;
            _workContext = workContext;
            _paymentRepository = paymentRepository;
            _currencyService = currencyService;
            _paymentProviderRepository = paymentProviderRepository;
            _emailTemplateService = emailTemplateService;
            _emailSender = emailSender;
            _UserPlansService = UserPlansService;
            _transHistoryRepository = transHistoryRepository;
            _dcmsLogService = dcmsLogService;
            _settingservice = settingservice;
            _assembly = Assembly.GetExecutingAssembly().GetName().Name;
            _context = context;
            _JeeblyService = JeeblyService;
            _orderRepository = orderRepository;
            _paymentrefundRepository = paymentrefundRepository;
            _planServices = planServices;
            _userplanRepository = userplanRepository;
            _userplanHistoryRepository = userplanHistoryRepository;
            _UserBookingService = UserBookingService;
            _testNotifier = testNotifier;
            this.SubscriptionServices = SubscriptionServices;
            _httpClientFactory = httpClientFactory;
        }
        public async Task<TelrResponseVM> TelrPlanRequest(long SiteId, CartTypes CartType)
        {
            try
            {

                PlansVersionsOutput PlanVersionDetails = new PlansVersionsOutput();
                var currentUser = await _workContext.GetCurrentUser();
                var CultureId =  _workContext.GetCurrentCultureId();
                var cart = await _cartService.GetActiveCart(currentUser.Id, CartType, SiteId);
                string PlanName = string.Empty;
                if (cart == null)
                {
                    throw new Exception("Cart is empty");
                }
                var orderCreationResult = await _orderService.CreateOrder(cart.Id, "TELR", 0, SiteId, OrderStatus.PendingPayment);
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                if (!orderCreationResult.Success)
                    throw new Exception(orderCreationResult.Error);

                var order = orderCreationResult.Value;

                var payment = new Payment()
                {
                    OrderId = order.Id,
                    PaymentFee = order.PaymentFeeAmount,
                    Amount = order.OrderTotal,
                    PaymentMethod = "Telr",
                    CreatedOn = DateTimeOffset.UtcNow,
                };

                var zeroDecimalOrderAmount = orderCreationResult.Value.OrderTotal;

                var settings = await GetTelrPaymentSettings(SiteId);

               //Which Plan Used
                await SubscriptionServices.HandleRegularPlans(cart, SiteId, orderCreationResult.Value.Id, PlanVersionDetails, orderCreationResult.Value);

                List<KeyValuePair<string, string>> Keys = new List<KeyValuePair<string, string>>();

                if (SubscriptionServices.IsApplicableCartType(CartType)) // This means a recurring order
                {
                    string repeatStart = SubscriptionServices.CalculateRepeatStart(cart.PlansVersion.Plan.PlanType, PlanVersionDetails.RepeatPeriod, PlanVersionDetails.RepeatInterval);

                    Keys = GenerateCommonKeyValues(orderCreationResult, currentUser, settings, zeroDecimalOrderAmount);

                    Keys.AddRange(new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("repeat_amount", orderCreationResult.Value.OrderTotal.ToString()),
                        new KeyValuePair<string, string>("repeat_period", PlanVersionDetails.RepeatPeriod.ToString()),
                        new KeyValuePair<string, string>("repeat_interval", PlanVersionDetails.RepeatInterval.ToString()),
                        new KeyValuePair<string, string>("repeat_start", repeatStart),
                        new KeyValuePair<string, string>("repeat_term", PlanVersionDetails.RepeatTerm.ToString()),
                        new KeyValuePair<string, string>("repeat_final", PlanVersionDetails.RepeatFinal.ToString()),
                        new KeyValuePair<string, string>("ivp_framed", "2"),

                    });
                }
            
                else
                {
                    Keys = GenerateCommonKeyValues(orderCreationResult, currentUser, settings, zeroDecimalOrderAmount);
                    Keys.AddRange(new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("ivp_desc", "HomeCutsShopOrder : " + string.Join(',', orderCreationResult.Value.OrderItems.SelectMany(x => x.Product.ProductDetails).Where(x => x.CultureId == CultureId).ToList()))

                    });

                }
                using (var client = _httpClientFactory.CreateClient())
                {
                    client.BaseAddress = new Uri(settings.BaseAddress);
                    client.DefaultRequestHeaders.ExpectContinue = false;
                    using var result = await client.PostAsync(settings.URLType,
                    new FormUrlEncodedContent(Keys));

                    if (result.IsSuccessStatusCode)
                    {
                        string val = string.Empty;
                        string data = await result.Content.ReadAsStringAsync();
                        var Resobj = JsonConvert.DeserializeObject<TelrResponseVM>(data);

                        if (Resobj.error != null)
                        {
                            await _dcmsLogService.AddError(currentUser.Id, _assembly, "TelrPlanRequest", Resobj.error.message, Resobj.error.note, "traceid :" + Resobj.trace, SiteId);
                            throw new Exception(Resobj.error.message + " " + Resobj.error.note);
                        }
                        if (Resobj.order != null)
                        {
                            await _dcmsLogService.AddInfo(currentUser.Id, _assembly, "traceid :" + Resobj.trace, "Success", SiteId);

                            string PaymentRefId = Resobj.order.@ref;
                            payment.Status = PaymentStatus.Initiated;
                            payment.GatewayTransactionId = PaymentRefId;
                            _paymentRepository.Add(payment);
                            order.OrderStatus = OrderStatus.New;
                            await _paymentRepository.SaveChangesAsync();

                            TelrAdviceVM objadvise = new TelrAdviceVM();
                            objadvise.tran_cartid = orderCreationResult.Value.Id.ToString();
                            objadvise.tran_store = settings.StoreId;
                            if (CartType == CartTypes.Plan)
                            {
                                objadvise.tran_desc = PlanVersionDetails.Plan.Name;
                            }
                            objadvise.tran_test = settings.TestMode;
                            objadvise.tran_amount = zeroDecimalOrderAmount.ToString();
                            objadvise.tran_currency = orderCreationResult.Value.CurrencyCode;
                            objadvise.bill_title = orderCreationResult.Value.BillingAddress.ContactName;
                            objadvise.bill_fname = orderCreationResult.Value.BillingAddress.ContactName;
                            objadvise.bill_sname = orderCreationResult.Value.BillingAddress.ContactName;
                            objadvise.bill_addr1 = orderCreationResult.Value.BillingAddress.AddressLine1;
                            objadvise.bill_addr2 = orderCreationResult.Value.BillingAddress.AddressLine1;
                            objadvise.bill_city = orderCreationResult.Value.BillingAddress.City;
                            objadvise.bill_region = orderCreationResult.Value.BillingAddress.City;
                            objadvise.bill_country = orderCreationResult.Value.BillingAddress.CountryId;
                            objadvise.bill_zip = orderCreationResult.Value.BillingAddress.ZipCode;
                            objadvise.bill_email = orderCreationResult.Value.BillingAddress.Email;

                            await AddTelrTransactionHistory(objadvise, payment.Id);

                            await transaction.CommitAsync();
                            Resobj.order.id = order.Id;
                            return Resobj;
                        }
                        return new TelrResponseVM();

                    }
                    return new TelrResponseVM();
                }
                }); // end strategy.ExecuteAsync
            }
            catch (Exception exc)
            {
                await _dcmsLogService.AddError(await _workContext.GetCurrentUserId(), _assembly, "TelrPlanRequest", exc.Message, exc.Message, "traceid :" + "", SiteId);
                throw;

            }
        }

        public async Task RegisterTelrPayment(Dictionary<string, string> dic, long SiteId)
        {
            TelrAdviceVM TelrResp = null;
            try
            {
                TelrResp = GetObject<TelrAdviceVM>(dic);
                if (TelrResp != null)
                {
                    var Req = JsonConvert.SerializeObject(dic, new JsonSerializerSettings()
                    {
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                        Formatting = Newtonsoft.Json.Formatting.Indented
                    });
                    await _dcmsLogService.AddInfo(null, _assembly, "RegisterTelrPayment", long.Parse(TelrResp.tran_cartid), TelrResp.tran_authmessage, "RegisterTelrPayment", null, "Request Started", Req, null);
                    if (await IsExternalRequestAuthorised(TelrResp))
                    {
                        var Order = await _orderService.GetOrderById(long.Parse(TelrResp.tran_cartid));
                        if (Order == null)
                        {
                            await _dcmsLogService.AddError(_assembly, "Order is not found", "", TelrResp.ToString(), null);

                            throw new Exception("Order not found");
                        }
                        var payment = await _paymentRepository.Query().Where(x => x.OrderId == Order.Id).FirstOrDefaultAsync();
                        if (payment == null)
                        {
                            await _dcmsLogService.AddError(_assembly, "Payment is not found", TelrResp.tran_cartid, TelrResp.ToString(), null);

                            throw new Exception("Payment is not found");
                        }

                        if (TelrResp.tran_status == "A")
                        {

                         //   await SubscriptionServices.UpdateUserPlanStatus(Order.Id);


                            // } //Do nothing since if its a normal order
                            await _paymentRepository.ExecuteInTransactionAsync(async () =>
                            {
                            payment.GatewayTransactionId = TelrResp.tran_ref;
                            payment.Status = PaymentStatus.Succeeded;
                            _paymentRepository.Update(payment);
                            await _paymentRepository.SaveChangesAsync();
                            await _orderService.OrderChangeStatus(Order.Id, OrderStatus.PaymentReceived, "Telr Advise Service", 1);

                            /// await _JeeblyService.CreateShipmentOrder(Order);
                            });
                            await _dcmsLogService.AddInfo(null, _assembly, "TelrPayment", long.Parse(TelrResp.tran_cartid), TelrResp.tran_authmessage, "RegisterTelrPayment", null, "AddSucsess", "null", null);
                        }
                        else if (TelrResp.tran_status == "H")
                        {

                            await _paymentRepository.ExecuteInTransactionAsync(async () =>
                            {
                            payment.GatewayTransactionId = TelrResp.tran_ref;
                            payment.Status = PaymentStatus.OnHold;
                            payment.FailureMessage = TelrResp.tran_authmessage;
                            _paymentRepository.Update(payment);
                            await _orderService.OrderChangeStatus(Order.Id, OrderStatus.OnHold, "Telr Advise Service", 1);
                            await _paymentRepository.SaveChangesAsync();
                            });

                            await _dcmsLogService.AddInfo(null, _assembly, "Payment Status", long.Parse(TelrResp.tran_cartid), TelrResp.tran_authmessage, "RegisterTelrPayment", null, PaymentStatus.OnHold.ToString(), null, null);
                        }
                        else
                        {
                            await _paymentRepository.ExecuteInTransactionAsync(async () =>
                            {

                            payment.GatewayTransactionId = TelrResp.tran_ref;
                            payment.Status = PaymentStatus.Failed;
                            payment.FailureMessage = TelrResp.tran_authmessage;
                            _paymentRepository.Update(payment);

                            await _orderService.OrderChangeStatus(Order.Id, OrderStatus.PaymentFailed, "Telr Advise Service", 1);

                            await _paymentRepository.SaveChangesAsync();
                            });


                        }

                        await AddTelrTransactionHistory(TelrResp, payment.Id);
                    }
                }
            }
            catch (Exception exc)
            {
                await _dcmsLogService.AddError("TelrPayment", exc.Message, exc.Message, "Service exception RegisterTelrPayment", null);

                var emailTemplateObj = await _emailTemplateService.GetEmailTemplateById("TelrRegistrationError");
                string EmailBody = "Telr cart Id : " + TelrResp.tran_cartid + " has been failed with message : " + exc.Message;
                string EmailSubject = "Failed to Update Order Status";
                string CartId = string.Empty;

                if (emailTemplateObj != null && emailTemplateObj.EmailBody != null)
                {
                    EmailSubject = emailTemplateObj.Subject;
                    EmailBody = emailTemplateObj.EmailBody.Replace("[TelrCartId]", TelrResp.tran_cartid);
                    EmailBody += emailTemplateObj.EmailBody.Replace("[TelrException]", exc.Message);
                    EmailBody += emailTemplateObj.EmailBody.Replace("[CustomerName]", TelrResp.bill_fname + " " + TelrResp.bill_sname);
                    EmailBody += emailTemplateObj.EmailBody.Replace("[PlanName]", TelrResp.tran_desc);
                    EmailBody += emailTemplateObj.EmailBody.Replace("[LastFourDigits]", TelrResp.card_last4);
                    EmailBody += emailTemplateObj.EmailBody.Replace("[OrderAmount]", TelrResp.tran_amount);
                    EmailBody += emailTemplateObj.EmailBody.Replace("[Phone]", TelrResp.bill_phone1);
                    EmailBody += emailTemplateObj.EmailBody.Replace("[TelrDesc]", TelrResp.tran_desc);
                }

                var settings = await _settingservice.GetSiteSettingById("PaymentHold", SiteId);
                var emailSetting = JsonConvert.DeserializeObject<TelEmailVM>(settings.Value);
                List<string> adminEmails = new List<string>();
                List<string> customerEmails = new List<string>();
                adminEmails.Add(emailSetting.Value);

                // await _emailSender.SendEmailAsync(adminEmails, EmailSubject, EmailBody, SiteId);

                NotificationObject data = new NotificationObject();
                List<long> Ids = new List<long>();
                Ids.Add(await _workContext.GetCurrentUserId());
                data.UserId = Ids.ToArray();
                data.MessageTitle = EmailSubject;
                data.MessageBody = EmailBody;
                data.SendValue = adminEmails;
                data.Severity = NotificationSeverity.Error;
                //data.EmailOptions = data.EmailSenderVM;
                await _testNotifier.SendMessageAsync(NotificationType.Email, null, data, 0);

                await _dcmsLogService.AddError("TelrPayment", exc.Message, exc.Message, "Service exception RegisterTelrPayment", null);
                throw exc;
            }
        }

        public async Task PaymentCancelation(long UserPlanId, long SiteId)
        {
            try
            {
                var UserPlan = await _UserPlansService.GetUserPlanByUserPlanId(UserPlanId);
                if (UserPlan == null)
                    throw new Exception("User plan dosnt exist");

                var settings = await _settingservice.GetSiteSettingsByModule("TelrPayment", SiteId);


                bool IsRefundSucess = await PaymentRefund(UserPlan, SiteId);

                //
                var TransHistory = await GetAgrrementId(UserPlan.Order.Id, SiteId, settings);

                using (var httpClient = _httpClientFactory.CreateClient())
                {
                    var Key = settings.Where(x => x.Id == "AuthUsername").Select(x => x.Value).FirstOrDefault() + ":" + settings.Where(x => x.Id == "AuthPassword").Select(x => x.Value).FirstOrDefault();

                    var plainTextBytes = Encoding.UTF8.GetBytes(Key);
                    string val = System.Convert.ToBase64String(plainTextBytes);
                    httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + val);


                    var PrepareURL = settings.Where(x => x.Id == "CancelAgreement").Select(x => x.Value).FirstOrDefault();// "https://secure.telr.com/tools/api/xml/agreement/:agreement_id";//
                    PrepareURL = PrepareURL.Replace(":agreement_id", TransHistory.ToString());
                    using (var response = await httpClient.DeleteAsync(PrepareURL))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            XmlRootAttribute xRoot = new XmlRootAttribute();
                            xRoot.ElementName = "transactions";
                            // xRoot.Namespace = "http://www.cpandl.com";
                            xRoot.IsNullable = true;

                            string apiResponse = await response.Content.ReadAsStringAsync();


                            await _dcmsLogService.AddInfo(null, _assembly, "TransactionSearchCartId", "Success", null);
                           await SubscriptionServices.CnacelUserPlan(UserPlan, IsRefundSucess);
                        }
                        else
                        {
                            throw new Exception(response.StatusCode.ToString());
                        }
                    }
                }
            }
            catch (Exception exc)
            {

                throw exc;

            }
        }
        public async Task<TransactionsVm> GetTransactionsSearch(long OrderId, long SiteId)
        {
            try
            {
                //  var Order = await _orderService.GetOrderDetailsById(OrderId);

                var HistoryOrderId = await _transHistoryRepository.Query().Where(x => x.OrderId == OrderId).FirstOrDefaultAsync();

                var settings = await _settingservice.GetSiteSettingsByModule("TelrPayment", SiteId);
                TransactionsVm returnObj = new TransactionsVm();
                using (var httpClient = _httpClientFactory.CreateClient())
                {
                    var plainTextBytes = Encoding.UTF8.GetBytes(/*settings.Select(x => x.Id == "AuthUsername")*/"15156:107e0759M$LBM6T7gTk#9b2q");
                    string val = System.Convert.ToBase64String(plainTextBytes);
                    httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + val);


                    var PrepareURL = "https://secure.telr.com/tools/api/xml/transaction/:ref/cart";// settings.Select(x => x.Id == "TransactionSearchCartId");
                    PrepareURL = PrepareURL.Replace(":ref", HistoryOrderId.TransCartId);
                    var response = await httpClient.GetAsync(PrepareURL);


                    if (response.IsSuccessStatusCode)
                    {
                        XmlRootAttribute xRoot = new XmlRootAttribute();
                        xRoot.ElementName = "transactions";
                        // xRoot.Namespace = "http://www.cpandl.com";
                        xRoot.IsNullable = true;
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        var Req = JsonConvert.SerializeObject(apiResponse, new JsonSerializerSettings()
                        {
                            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                            Formatting = Newtonsoft.Json.Formatting.Indented
                        });

                        await _dcmsLogService.AddInfo(null, _assembly, "GetTelrTransByOrderId", OrderId, "", "TerlTransHistory", null, "GetTransactionsSearch", Req, null);


                        XmlSerializer serializer = new XmlSerializer(typeof(TransactionsVm), xRoot);
                        using (Stream stream = await response.Content.ReadAsStreamAsync())
                        {
                            await _dcmsLogService.AddInfo(null, _assembly, "TransactionSearchCartId", "Success", null);
                            returnObj = (TransactionsVm)serializer.Deserialize(stream);
                            HistoryOrderId.AgreementId = returnObj.transaction[0].agreementid;

                            _transHistoryRepository.Update(HistoryOrderId);

                            await _transHistoryRepository.SaveChangesAsync();

                        }

                    }
                    else
                    {
                        throw new Exception(response.StatusCode.ToString());
                    }


                    return returnObj;
                }


            }
            catch (Exception exc)
            {
                await _dcmsLogService.AddError(_assembly, exc.Message, "", "", null);
                throw exc;
            }
        }


        #region helpers

        private async Task<long> GetAgrrementId(long OrderId, long SiteId, List<SiteSettingsVM> settings)
        {
            try
            {
                //  var Order = await _orderService.GetOrderDetailsById(OrderId);

                var HistoryOrderId = await _transHistoryRepository.Query().Where(x => x.OrderId == OrderId).FirstOrDefaultAsync();
                await _dcmsLogService.AddError(null, _assembly, "", "HistoryOrderId : " + HistoryOrderId.ToString(), SiteId);
                //var settings = await _settingservice.GetSiteSettingsByModule("TelrPayment", SiteId);
                TransactionsVm returnObj = new TransactionsVm();
                using (var httpClient = _httpClientFactory.CreateClient())
                {
                    var Key = settings.Where(x => x.Id == "AuthUsername").Select(x => x.Value).FirstOrDefault() + ":" + settings.Where(x => x.Id == "AuthPassword").Select(x => x.Value).FirstOrDefault();
                    var plainTextBytes = Encoding.UTF8.GetBytes(Key); // "15156:107e0759M$LBM6T7gTk#9b2q");
                    string val = System.Convert.ToBase64String(plainTextBytes);
                    httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + val);


                    var PrepareURL = settings.Where(x => x.Id == "TransactionSearchCartId").Select(x => x.Value).FirstOrDefault().ToString(); //"https://secure.telr.com/tools/api/xml/transaction/:ref/cart";// 
                    PrepareURL = PrepareURL.Replace(":ref", HistoryOrderId.TransCartId);
                    var response = await httpClient.GetAsync(PrepareURL);
                    await _dcmsLogService.AddInfo(null, _assembly, "response", response.ToString(), null);
                    if (response.IsSuccessStatusCode)
                    {
                        XmlRootAttribute xRoot = new XmlRootAttribute();
                        xRoot.ElementName = "transactions";
                        // xRoot.Namespace = "http://www.cpandl.com";
                        xRoot.IsNullable = true;
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        var Req = JsonConvert.SerializeObject(apiResponse, new JsonSerializerSettings()
                        {
                            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                            Formatting = Newtonsoft.Json.Formatting.Indented
                        });

                        await _dcmsLogService.AddInfo(null, _assembly, "GetAgrrementId", OrderId, "", "Agreement", null, "GetAgreement", Req, null);


                        XmlSerializer serializer = new XmlSerializer(typeof(TransactionsVm), xRoot);
                        using (Stream stream = await response.Content.ReadAsStreamAsync())
                        {
                            await _dcmsLogService.AddInfo(null, _assembly, "TransactionSearchCartId", "Success", null);
                            returnObj = (TransactionsVm)serializer.Deserialize(stream);
                            HistoryOrderId.AgreementId = returnObj.transaction[0].agreementid;

                            _transHistoryRepository.Update(HistoryOrderId);

                            await _transHistoryRepository.SaveChangesAsync();

                        }

                    }
                    else
                    {
                        throw new Exception(response.StatusCode.ToString());
                    }
                    long AgreementId = 0;
                    return AgreementId = long.Parse(returnObj.transaction[0].agreementid);


                }


            }
            catch (Exception exc)
            {
                await _dcmsLogService.AddError(_assembly, exc.Message, "", "", null);
                throw exc;
            }
        }

        private T GetObject<T>(Dictionary<string, string> dict)
        {
            System.Type type = typeof(T);
            var obj = Activator.CreateInstance(type);

            foreach (var kv in dict)
            {
                type.GetProperty(kv.Key).SetValue(obj, kv.Value);
            }
            return (T)obj;
        }

        private async Task<TelrSettingsVM> GetTelrPaymentSettings(long SiteId)
        {
            try
            {
                var providers = await _paymentProviderRepository.Query()
                    .Where(x => x.SiteId == SiteId && x.Id == "TELR")
                    .Select(x => x.AdditionalSettings)
                    .FirstOrDefaultAsync();
                return JsonConvert.DeserializeObject<TelrSettingsVM>(providers);
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
        private async Task<bool> IsExternalRequestAuthorised(TelrAdviceVM TelrResp)
        {
            try
            {
                string SecretKey = (await GetTelrPaymentSettings(1)).DataIntegritySecretkey;// StoreId/*"WcVJh#b2#"*/;
                string SecureHash = TelrResp.tran_check;
                string Key = "";
                StringBuilder builder = new StringBuilder();
                builder.Append(SecretKey);
                builder.Append(":" + TelrResp.tran_store);
                builder.Append(":" + TelrResp.tran_type);
                builder.Append(":" + TelrResp.tran_class);
                builder.Append(":" + TelrResp.tran_test);
                builder.Append(":" + TelrResp.tran_ref);
                builder.Append(":" + TelrResp.tran_prevref);
                builder.Append(":" + TelrResp.tran_firstref);
                builder.Append(":" + TelrResp.tran_currency);
                builder.Append(":" + TelrResp.tran_amount);
                builder.Append(":" + TelrResp.tran_cartid);
                builder.Append(":" + TelrResp.tran_desc);
                builder.Append(":" + TelrResp.tran_status);
                builder.Append(":" + TelrResp.tran_authcode);
                builder.Append(":" + TelrResp.tran_authmessage);

                Key = SHA256Signature.CreateSha1(builder.ToString());

                if (Key.ToUpper() != SecureHash.ToUpper())
                    throw new Exception("Request is not authorized");
                else
                    return true;

            }
            catch (Exception exc)
            {
                await _dcmsLogService.AddError("TelrPayment", exc.Message, exc.Message, "Is external request authorised", 1);
                throw exc;
            }
        }
        private List<KeyValuePair<string, string>> GenerateCommonKeyValues(Result<Order> orderCreationResult, User currentUser, TelrSettingsVM settings, decimal zeroDecimalOrderAmount)
        {
            return new List<KeyValuePair<string, string>>
            {
                        new KeyValuePair<string, string>("ivp_method", "create"),
                        new KeyValuePair<string, string>("ivp_store", settings.StoreId),
                        new KeyValuePair<string, string>("ivp_authkey",settings.Authkey),
                        new KeyValuePair<string, string>("ivp_cart",orderCreationResult.Value.Id.ToString()),
                        new KeyValuePair<string, string>("ivp_test",settings.TestMode),
                        new KeyValuePair<string, string>("ivp_amount",zeroDecimalOrderAmount.ToString()),
                        new KeyValuePair<string, string>("ivp_currency",orderCreationResult.Value.CurrencyCode),
                        new KeyValuePair<string, string>("return_auth", settings.return_auth),
                        new KeyValuePair<string, string>("return_can", settings.return_can),
                        new KeyValuePair<string, string>("return_decl",settings.return_decl),
                        new KeyValuePair<string, string>("bill_title",orderCreationResult.Value.BillingAddress.ContactName ),
                        new KeyValuePair<string, string>("bill_fname",currentUser. FullName),
                        new KeyValuePair<string, string>("bill_sname",orderCreationResult.Value.BillingAddress.ContactName  ),
                        new KeyValuePair<string, string>("bill_addr1",orderCreationResult.Value.BillingAddress.AddressLine1),
                        new KeyValuePair<string, string>("bill_addr2",orderCreationResult.Value.BillingAddress.AddressLine2),
                        new KeyValuePair<string, string>("bill_city", orderCreationResult.Value.BillingAddress.City),
                        new KeyValuePair<string, string>("bill_region",orderCreationResult.Value.BillingAddress.City),
                        new KeyValuePair<string, string>("bill_country", orderCreationResult.Value.BillingAddress.CountryId),
                        new KeyValuePair<string, string>("bill_zip", orderCreationResult.Value.BillingAddress.ZipCode),
                        new KeyValuePair<string, string>("bill_email", currentUser.Email),
                        new KeyValuePair<string, string>("bill_phone1", currentUser.PhoneNumber),
                        new KeyValuePair<string,string>("bill_custref",orderCreationResult.Value.CustomerId.ToString())

        };
        }
        private async Task AddTelrTransactionHistory(TelrAdviceVM TransHistory, long? PaymentId)
        {
            try
            {


                var objTlerTrans = await _transHistoryRepository.Query().Where(z => z.OrderId == Convert.ToInt64(TransHistory.tran_cartid)).FirstOrDefaultAsync();
                if (objTlerTrans != null)
                {
                    //TransactionHistory objTlerTrans = new TransactionHistory();
                    objTlerTrans.OrderId = long.Parse(TransHistory.tran_cartid);
                    objTlerTrans.TelrStoreId = long.Parse(TransHistory.tran_store);
                    objTlerTrans.TransClass = TransHistory.tran_class;
                    objTlerTrans.TransFirstRefId = long.Parse(TransHistory.tran_firstref);
                    objTlerTrans.TransPrevRefId = long.Parse(TransHistory.tran_prevref);
                    objTlerTrans.TransRefId = long.Parse(TransHistory.tran_ref);
                    objTlerTrans.TransTest = (TelrEnums.TelrTransTest)Enum.ToObject(typeof(TelrEnums.TelrTransTest), Convert.ToInt32(TransHistory.tran_test));
                    objTlerTrans.TransType = TransHistory.tran_type;
                    objTlerTrans.AgreementId = TransHistory.agreement_id;
                    objTlerTrans.TransCartId = TransHistory.tran_cartid;
                    objTlerTrans.TransStatus = TransHistory.tran_status;

                    objTlerTrans.TransAuthCode = TransHistory.tran_authcode;
                    objTlerTrans.TransAuthMessage = TransHistory.tran_authmessage;
                    objTlerTrans.TransCheck = TransHistory.tran_check;
                    objTlerTrans.CardCode = TransHistory.card_code;
                    objTlerTrans.CardLast4 = TransHistory.card_last4;
                    objTlerTrans.IntegrationId = long.Parse(TransHistory.integration_id);

                    objTlerTrans.PaymentId = PaymentId;

                    objTlerTrans.CardCheck = TransHistory.card_check;
                    objTlerTrans.CreationDate = DateTimeOffset.Now;
                    objTlerTrans.ParentId = objTlerTrans.Id;
                    _transHistoryRepository.Update(objTlerTrans);
                    await _transHistoryRepository.SaveChangesAsync();
                }
                else
                {


                    TransactionHistory objlocaTlerTrans = new TransactionHistory();
                    objlocaTlerTrans.OrderId = long.Parse(TransHistory.tran_cartid);
                    objlocaTlerTrans.TelrStoreId = long.Parse(TransHistory.tran_store);
                    objlocaTlerTrans.TransClass = TransHistory.tran_class;
                    //objlocaTlerTrans.TransFirstRefId = long.Parse(TransHistory.tran_firstref);
                    //objlocaTlerTrans.TransPrevRefId = long.Parse(TransHistory.tran_prevref);
                    //objlocaTlerTrans.TransRefId = long.Parse(TransHistory.tran_ref);
                    objlocaTlerTrans.TransTest = (TelrEnums.TelrTransTest)Enum.ToObject(typeof(TelrEnums.TelrTransTest), Convert.ToInt32(TransHistory.tran_test));
                    objlocaTlerTrans.TransType = TransHistory.tran_type;
                    objlocaTlerTrans.AgreementId = TransHistory.agreement_id;
                    objlocaTlerTrans.TransCartId = TransHistory.tran_cartid;
                    objlocaTlerTrans.TransStatus = TransHistory.tran_status;

                    objlocaTlerTrans.TransAuthCode = TransHistory.tran_authcode;
                    objlocaTlerTrans.TransAuthMessage = TransHistory.tran_authmessage;
                    objlocaTlerTrans.TransCheck = TransHistory.tran_check;
                    objlocaTlerTrans.CardCode = TransHistory.card_code;
                    objlocaTlerTrans.CardLast4 = TransHistory.card_last4;
                    //  objlocaTlerTrans.IntegrationId = long.Parse(TransHistory.integration_id);
                    objlocaTlerTrans.PaymentId = PaymentId;
                    objlocaTlerTrans.CardCheck = TransHistory.card_check;
                    objlocaTlerTrans.ParentId = null;
                    _transHistoryRepository.Add(objlocaTlerTrans);
                    await _transHistoryRepository.SaveChangesAsync();
                }
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        private async Task<bool> PaymentRefund(UserPlansOutput UserPlan, long SiteId)
        {
            try
            {

                if (UserPlan == null)
                    throw new Exception("User plan dosnt exist");

                UserBooking userBooking = await _UserBookingService.GetByUserPlanId(UserPlan.Id);
                if (UserPlan.ActivationDate.AddDays(UserPlan.PlanVersions.RefundPeriod) < DateTimeOffset.Now)
                {
                    await _dcmsLogService.AddInfo(null, _assembly, "Refund plan activation date", "UserPlanId : " + UserPlan.Id.ToString() + " plan activation date " + UserPlan.ActivationDate.AddDays(UserPlan.PlanVersions.RefundPeriod).ToString() + " Period " + UserPlan.PlanVersions.RefundPeriod.ToString(), SiteId);
                    return false;
                }

                if (userBooking != null)
                {

                    await _dcmsLogService.AddInfo(null, _assembly, "Refund plan activation date", "UserPlanId : " + UserPlan.Id.ToString() + " plan activation date " + UserPlan.ActivationDate.AddDays(UserPlan.PlanVersions.RefundPeriod).ToString() + " Period " + UserPlan.PlanVersions.RefundPeriod.ToString(), SiteId);
                    return false;
                }

                var settings = await _settingservice.GetSiteSettingById("RefundPayment", SiteId);
                var RefundSetting = JsonConvert.DeserializeObject<RefundVM>(settings.Value);
                using (var httpClient = _httpClientFactory.CreateClient())
                {
                    EskaCommerce.TelrPayment.Models.Refund.remote telrRefundVM = new EskaCommerce.TelrPayment.Models.Refund.remote();
                    telrRefundVM.store = RefundSetting.StoreId;// UserPlan.Order.Id.ToString();
                    telrRefundVM.key = RefundSetting.Key;
                    telrRefundVM.tran.type = RefundSetting.Type;
                    telrRefundVM.tran.@class = RefundSetting.@class;
                    telrRefundVM.tran.cartid = UserPlan.Order.Id.ToString();
                    telrRefundVM.tran.description = "Order Id - " + UserPlan.Order.Id.ToString();
                    telrRefundVM.tran.test = RefundSetting.Test;
                    telrRefundVM.tran.currency = RefundSetting.Currency;
                    telrRefundVM.tran.amount = UserPlan.ParentId.HasValue ? UserPlan.ParentAmount : UserPlan.Order.OrderTotal.ToString();
                    var Payment = await _paymentRepository.Query().Where(x => x.OrderId == UserPlan.Order.Id).FirstOrDefaultAsync();
                    telrRefundVM.tran.@ref = Payment.GatewayTransactionId;
                    await _dcmsLogService.AddInfo(null, _assembly, "Gateway Transaction Id" + telrRefundVM.tran.@ref, SiteId);

                    var PrepareURL = RefundSetting.URL;// "https://secure.telr.com/gateway/remote.xml";// settings.Select(x => x.Id == "TransactionSearchCartId");

                    XmlSerializer xmlSerializer = new XmlSerializer(telrRefundVM.GetType());
                    string result;
                    Encoding utf8noBOM = new UTF8Encoding(false);
                    XmlWriterSettings wrtiterSettings = new XmlWriterSettings();
                    wrtiterSettings.Indent = true;
                    wrtiterSettings.Encoding = utf8noBOM;
                    using (MemoryStream output = new MemoryStream())
                    {
                        using (XmlWriter writer = XmlWriter.Create(output, wrtiterSettings))
                        {
                            xmlSerializer.Serialize(writer, telrRefundVM);
                        }
                        result = Encoding.Default.GetString(output.ToArray());
                    }

                    HttpContent content = new StringContent(result, Encoding.UTF8, "application/xml");

                    using (var response = await httpClient.PostAsync(PrepareURL, content))
                    {
                        if (response.IsSuccessStatusCode)
                        {

                            XmlRootAttribute xRoot = new XmlRootAttribute();
                            xRoot.ElementName = "remote";

                            xRoot.IsNullable = true;

                            string apiResponse = await response.Content.ReadAsStringAsync();
                            XmlSerializer serializer = new XmlSerializer(typeof(remote), xRoot);
                            using (Stream stream = await response.Content.ReadAsStreamAsync())
                            {


                                var responseobject = (remote)serializer.Deserialize(stream);
                                PaymentRefund objrefund = new PaymentRefund();

                                if (responseobject.auth.status == "A")
                                {
                                    objrefund.PaymentId = Payment.Id;
                                    objrefund.RefundStatus = RefundPaymentStatus.Succeeded;
                                    objrefund.Tranref = responseobject.auth.tranref;
                                    objrefund.AdditionalInfo = responseobject.ToJsonString();

                                    await _dcmsLogService.AddInfo(null, _assembly, "Refund Success", "UserPlanId : " + UserPlan.Id.ToString(), SiteId);
                                    return true;
                                }
                                else
                                {
                                    objrefund.PaymentId = Payment.Id;
                                    objrefund.RefundStatus = RefundPaymentStatus.Failed;
                                    objrefund.Tranref = responseobject.auth.tranref;
                                    objrefund.AdditionalInfo = responseobject.ToJsonString();
                                }
                                _paymentrefundRepository.Add(objrefund);
                                await _paymentrefundRepository.SaveChangesAsync();

                                await _dcmsLogService.AddInfo(null, _assembly, "IsSuccessStatusCode",
                              response.IsSuccessStatusCode.ToString()
                              + "responseobject " + responseobject.ToJsonString() + " objrefund " + objrefund + " Refund Failed" + " UserPlanId : " + UserPlan.Id.ToString(),
                              SiteId);


                            }

                        }
                        else
                        {
                            await _dcmsLogService.AddError(null, _assembly, "Last line Refund Failed", "UserPlanId : " + UserPlan.Id.ToString(), SiteId);


                            throw new Exception(response.StatusCode.ToString());
                        }

                    }

                }
                return true;
            }
            catch (Exception exc)
            {
                var setting = await _settingservice.GetSiteSettingById("PaymentHold", 1);
                var emailSetting = JsonConvert.DeserializeObject<TelEmailVM>(setting.Value);
                List<string> adminEmails = new List<string>();
                adminEmails.Add(emailSetting.Value);


                NotificationObject data = new NotificationObject();
                List<long> Ids = new List<long>();
                Ids.Add(await _workContext.GetCurrentUserId());
                data.UserId = Ids.ToArray();
                data.MessageTitle = "Payment Refund Error failure" + "User: " + UserPlan.User.FullName;
                data.MessageBody = "Failed to refund plan payment, UserPlanId = " + UserPlan.Id + " PlanName : " + UserPlan.PlanName + "UserId" + UserPlan.UserId + "Error : " + exc.Message;
                data.SendValue = adminEmails;
                data.Severity = NotificationSeverity.Error;
                //data.EmailOptions = data.EmailSenderVM;
                await _testNotifier.SendMessageAsync(NotificationType.Email, null, data, 0);

                //await _emailSender.SendEmailAsync(adminEmails, "Payment Refund Error failure" + "User : " + UserPlan.User.FullName, "Failed to refund plan payment, UserPlanId = " + UserPlan.Id + " PlanName : " + UserPlan.PlanName + "UserId" + UserPlan.UserId + "Error : " + exc.Message, 1);

                throw exc;
            }


        }

        #endregion
    }



}


