using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Payments;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCommerce.Orders.Services;
using EskaCommerce.Payments.Models.PaymentMEPS;
using EskaCommerce.Payments.Services.PaymentMEPS.Interfaces;
using EskaCommerce.Payments.Models;
using EskaCommerce.Payments.Helper;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace EskaCommerce.Payments.Services.PaymentMEPS
{
    public class PaymentMepsService: IPaymentMepsService
    {
        private readonly IRepositoryWithTypedId<PaymentProvider, string> _paymentProviderRepository;
        private readonly IOrderService orderService;
        private readonly IHttpClientFactory _httpClientFactory;

        public PaymentMepsService(IRepositoryWithTypedId<PaymentProvider, string> paymentProviderRepository, IOrderService orderService, IHttpClientFactory httpClientFactory)
        {
            _paymentProviderRepository = paymentProviderRepository;
            this.orderService = orderService;
            _httpClientFactory = httpClientFactory;
        }
        public async Task<PayTabsConfig> GetPayTabsPaymentSettings(long SiteId, string PaymentProviderId)
        {
            try
            {
                var ProviderConfigurations = await _paymentProviderRepository.Query().
                    FirstOrDefaultAsync(x => x.Id.ToLower() == PaymentProviderId.ToLower() && x.SiteId == SiteId);
                
                if (ProviderConfigurations != null)
                {
                    var ProviderSettings = JsonConvert.DeserializeObject<PayTabsConfig>(ProviderConfigurations.AdditionalSettings);
                    return ProviderSettings;
                }
                else
               {
                    throw new Exception("No configurations for the this payment provider");
               }
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<MepsConfig> GetMepsPaymentSettings(long SiteId)
        {
            try
            {
                var ProviderConfigurations = await _paymentProviderRepository.Query().
                    FirstOrDefaultAsync(x => x.Id == PaymentProviderHelper.MepsProviderId && x.SiteId == SiteId);

                if (ProviderConfigurations != null)
                {
                    var ProviderSettings = JsonConvert.DeserializeObject<MepsConfig>(ProviderConfigurations.AdditionalSettings);

                    return ProviderSettings;
                }
                else
                {
                    throw new Exception("No configurations for the this payment provider");
                }
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }


        public async Task<NetworkConfig> GetNetowrkPaymentSettings(long siteId)
        {
            try
            {
                var ProviderConfigurations = await _paymentProviderRepository.Query().
                    FirstOrDefaultAsync(x => x.Id == PaymentProviderHelper.NetworkProviderId && x.SiteId == siteId);

                if (ProviderConfigurations != null)
                {
                    var ProviderSettings = JsonConvert.DeserializeObject<NetworkConfig>(ProviderConfigurations.AdditionalSettings);

                    return ProviderSettings;
                }
                else
                {
                    throw new Exception("No configurations for the this payment provider");
                }
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<bool> IsExternalRequestAuthorised(HttpContext context,long siteId ,string signature, string PaymentProviderId)
        {
            try
            {
                string SecretKey = GetPayTabsPaymentSettings(siteId, PaymentProviderId).Result.ServerKey;
                var request = context.Request;
              
                request.Body.Position = 0;
                
                using var reader = new StreamReader(context.Request.Body);
                
                var PayTabsRes = await reader.ReadToEndAsync();


                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(PayTabsRes));
                var  ContentSignature= BitConverter.ToString(hash).Replace("-", "").ToLower();
                if (ContentSignature.Equals(signature, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<IPNCallBack> PaytabsRecuring(long OrderId,string Token,string Tran_Refrance)
        {
            try
            {
                var Order = await orderService.GetOrderById(OrderId);

                var PayTabsSetting = await GetPayTabsPaymentSettings(Order.SiteId, Order.PaymentMethod);

                var Response = PrepareRequest(Tran_Refrance, Token, Order.CurrencyCode, Order, PayTabsSetting);
                return Response;

            }
            catch (Exception ex)
            {
                return new IPNCallBack();
            }         
        
        }
        

        private IPNCallBack PrepareRequest(string Tran_Refrance,string Token ,string ISOCurrencySymbol, Order order, PayTabsConfig PayTabsSetting)
        {
            try
            {
                PaymentTransactionRecuring objReq = new PaymentTransactionRecuring();
                objReq.profile_id = int.Parse(PayTabsSetting.ProfileId);
                objReq.tran_type = "sale";
                objReq.tran_class = "recurring";
                objReq.cart_amount = float.Parse((Math.Round(order.OrderTotal * 10) / 10).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
                objReq.cart_currency = ISOCurrencySymbol;
                objReq.cart_id = $"{PayTabsSetting.Environment}{order.Id}";
                objReq.cart_description = $"{PayTabsSetting.CartDesc}&{order.SiteId}";
                objReq.tran_ref = Tran_Refrance;
                objReq.token = Token;
             
                string result = string.Empty;
                string Json = JsonConvert.SerializeObject(objReq);
                HttpContent Obj = new StringContent(Json, Encoding.UTF8, "application/json");

                using (var client = _httpClientFactory.CreateClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Add("authorization", string.Format("{0}", PayTabsSetting.ServerKey));

                    HttpResponseMessage response = client.PostAsync(PayTabsSetting.PayTapsURL, Obj).Result;
                    result = response.Content.ReadAsStringAsync().Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = JsonConvert.DeserializeObject<IPNCallBack>(result);
                        return responseData;

                    }
                    else
                    {
                        throw new Exception(result.ToString());
                    }
                }




            }
            catch (Exception exc)
            {

                throw exc;
            }



        }

    }

}