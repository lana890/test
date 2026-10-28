using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Interfaces;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Integrations.Helpers;
using EskaCMS.Integrations.Models.Carseer;
using EskaCMS.Integrations.Models.CBVehicleAssessment;
using EskaCMS.Integrations.Models.Insurance;
using EskaCMS.Integrations.Services.JoClaim.Interface;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Notifiers;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels;
using EskaCommerce.ShoppingCart.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Integrations.Services.JoClaim
{
    public class JoClaimService: HttpServiceBase,IJoClaimService
	{
		public IntegrationSettings Settings { get; }
		private readonly IdcmsLogService _dcmsLogService;
		private readonly HttpClient _http;
        private readonly ICartService _cartService;
        private readonly IWorkContext _workContext;
        private readonly ITestNotifier _testNotifier;
        private readonly ISiteSettingsService _siteSettings;
		private readonly ISiteCultureService _siteCultureService;
		private readonly IRepository<CartItem> _cartItemRepository;

		private readonly CarseerSettings _carseerSettings;
        private readonly EskaCMS.Integrations.Models.JoClaim.JoClaim _joClaimSettings;
     
        public JoClaimService(IdcmsLogService dcmsLogService,IRepository<CartItem> cartItemRepository,ISiteCultureService siteCultureService,HttpClient http,IWorkContext workcontext,ICartService cartService,ITestNotifier testNotifier,ISiteSettingsService siteSettings, HttpClient client) : base(client)
		{
			Settings = IntegrationSettingsLoader.Load<IntegrationSettings>(AppContext.BaseDirectory, "Integration");
			Client.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Bearer", Settings.LongLifeToken);
            _cartItemRepository = cartItemRepository;
			_siteCultureService = siteCultureService;
			_http = http;
            _workContext = workcontext;
            _testNotifier = testNotifier;
            _cartService = cartService;
            _siteSettings = siteSettings;
            _dcmsLogService = dcmsLogService;
			_carseerSettings = IntegrationSettingsLoader.Load<CarseerSettings>(AppContext.BaseDirectory, "CarseerSettings");
            _joClaimSettings = IntegrationSettingsLoader.Load<EskaCMS.Integrations.Models.JoClaim.JoClaim>(AppContext.BaseDirectory, "JOClaimInteg");
        }
        public async Task<object> GetVehicleHistoryAsync(string vin, string language)
        {
            try
            {
                var apiKey = _carseerSettings.ApiKey;
                var baseURL = _carseerSettings.BaseURL;
                var queryUrl = $"{baseURL}/query?vin={vin}&lang={language}&key={apiKey}";
                var queryResponse = await _http.GetAsync(queryUrl);

                if (!queryResponse.IsSuccessStatusCode)
                    throw new Exception($"Carseer query failed with status {queryResponse.StatusCode}.");

                var queryBody = await queryResponse.Content.ReadAsStringAsync();
                var queryResult = JsonConvert.DeserializeObject<CarseerQueryResponse>(queryBody);  
                if (queryResult is null)
                    throw new Exception("Empty or malformed response from Carseer.");

                if (queryResult.Message.Equals("NO_DATA", StringComparison.OrdinalIgnoreCase) || queryResult.Products.Count == 0)
                    throw new HttpRequestException("No vehicle history report found for this VIN.", null, HttpStatusCode.NotFound);

                var serial = queryResult.Products[0].Serial;
                var fetchUrl = $"{baseURL}/Fetch?serial={serial}&lang={language}&key={apiKey}";
                var fetchResponse = await _http.GetAsync(fetchUrl);

                if (!fetchResponse.IsSuccessStatusCode)
                    throw new Exception($"Carseer fetch failed with status {fetchResponse.StatusCode}.");

                var fetchBody = await fetchResponse.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(fetchBody);
                return doc.RootElement.Clone();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task CBVehicleAssessment(long userId, string status,long cartId,long siteId)
        {
			//var siteId = await _workContext.GetCurrentSiteIdAsync();
			await _dcmsLogService.AddInfo(null, Assembly.GetExecutingAssembly().GetName().Name, $"JOClaim_{cartId}", cartId,"", "JOClaim", null, null, null, null);

			//_dcmsLogService.AddInfo(userId,$"JOClaim_CBVehicleAssessment.{cartId}", "JOClaim_CBVehicleAssessment", siteId);
			var currentCart = await _cartService.GetActiveCart(userId, CartTypes.Shop, siteId, cartId);
			var currentCartItem = await _cartItemRepository.Query().Where(x=>x.CartId ==cartId).FirstOrDefaultAsync();

			var additionalInfo = !string.IsNullOrWhiteSpace(currentCartItem?.AdditionalDetails)
				? JObject.Parse(currentCartItem.AdditionalDetails)
				: new JObject();



            //var overallStatus = reportJson
            //             .GetProperty("report")
            //             .GetProperty("casdamereport")
            //             .GetProperty("overallStatus")
            //             .GetString()

            // Hardcoded per request: force overallStatus = "3" (sent as ConditionPercentage / OverAllDamagePercent)
            var overallStatus = status.Replace("%", "").Trim();

            // Send percentage to Insurance Approval API
            var ApprovalApiUrl = _carseerSettings.ApprovalApiUrl;
            var approvalResponseList = await PostAsync<List<ApprovalResponse>>(ApprovalApiUrl, new ApprovalRequest
            {
                ConditionPercentage = overallStatus,
                AssesmentType = "1",
                OverAllDamagePercent = overallStatus,
			});
           var approvalResponse = approvalResponseList.FirstOrDefault();
			//approvalResponse.EnsureSuccessStatusCode();

			//var approval = await approvalResponse.Content.ReadFromJsonAsync<ApprovalResponse>()
			//    ?? throw new Exception("Empty response from approval API.");


			// Handle APPROVE / REJECT
			if (approvalResponse.ResultName.ToUpper().Equals("APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                await UpdateCartAsync("JoClaimStatus", "APPROVED",cartId);
				 await SendSmsAsync(additionalInfo.GetStringSafe("phone", "personalDetails.phone", "contactInfo.phone"), _joClaimSettings.hostName+ "/InspectionResult" + "?"+"cartId="+cartId, "APPROVE");
			}
            else if (approvalResponse.ResultName.ToUpper().Equals("REJECTED", StringComparison.OrdinalIgnoreCase))
            {
                await UpdateCartAsync("JoClaimStatus", "REJECTED",cartId);
            }
            else
            {
                throw new Exception($"Unexpected approval result: {approvalResponse.ResultName}");
            }
            //Convert assessment response to PDF and upload as attachment
            var pdfFile = ConvertToPdf(userId, overallStatus, approvalResponse.ResultName); 
            await UploadAttachmentAsync(userId.ToString(), pdfFile , additionalInfo);
        }
        public async Task<string> JoclaimInteg(JoclaimIntegRequest request,long? cartId)
        {
			var SiteId = _workContext.GetCurrentSiteId();

			var baseURL = _joClaimSettings.BaseURL;
            var callBackUrl = _joClaimSettings.CallBackUrl+"/"+SiteId+"/"+cartId;
			//Get access token
			var accessToken = await GetAccessTokenAsync(_joClaimSettings, baseURL, callBackUrl);
            //  Call SaveAICustomerRequest to get Joclaim URL
            var aiResponse = await SaveAICustomerRequestAsync(request.Vin, request.PlateNo, accessToken, baseURL);

            // Update cart with AICaseID
            await UpdateCartAsync("JoCliamID", aiResponse.AICaseID, cartId);

            // Send SMS with InspectionLink
            await SendSmsAsync(request.OwnerPhone, aiResponse.InspectionLink , "Vehicle Inspection");

            return aiResponse.InspectionLink;

		}
        // Authorization
        private async Task<string> GetAccessTokenAsync(EskaCMS.Integrations.Models.JoClaim.JoClaim JOClaimInteg, string? baseURl , string callBackApi)
        {
            var ClientId = JOClaimInteg.Client_Id ?? "";
            var ClientSecret = JOClaimInteg.Client_Secret ?? "";
            var grant_type = JOClaimInteg.Grant_Type ?? "";

            var formData = new Dictionary<string, string>
            {
                ["grant_type"] = grant_type,
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
                ["CallBackURL"] = callBackApi,
            };

            var response = await _http.PostAsync($"{baseURl}/oauth2/token", new FormUrlEncodedContent(formData));

            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();

			JoclaimTokenResponse token = JsonConvert.DeserializeObject<JoclaimTokenResponse>(body)
				?? throw new Exception($"Could not parse Joclaim auth response. Raw response: {body}");

			if (string.IsNullOrWhiteSpace(token.access_token))
                throw new Exception($"Joclaim auth succeeded but returned no access_token. Raw response: {body}");

            return token.access_token;
        }
        // SaveAICustomerRequest
        private async Task<SaveAICustomerResponse> SaveAICustomerRequestAsync(string vin, string plateNo, string accessToken, string? baseURL)
        {
            var url = $"{baseURL}/api/AI/SaveAICustomerRequest?Vin={Uri.EscapeDataString(vin)}&PlateNo={Uri.EscapeDataString(plateNo)}";

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
            httpRequest.Headers.Add("Authorization", $"bearer {accessToken}");

            var response = await _http.SendAsync(httpRequest);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<SaveAICustomerResponse>(body);  
                


            return result;
        }
        //Update cart
        private async Task UpdateCartAsync(string key, string value,long? cartId)
        {
            

           string payload = JsonConvert.SerializeObject(new Dictionary<string, string>
			{
				[key] = value
			});
            var userId = await _workContext.GetCurrentUserId();
            var SiteId = _workContext.GetCurrentSiteId();
            var currentCart = await _cartService.GetActiveCart(userId, CartTypes.Shop, SiteId, cartId);
			var currentCartItem = await _cartItemRepository.Query().Where(x => x.CartId == cartId).FirstOrDefaultAsync();

			UpdateCartInfoVM req = new UpdateCartInfoVM()
            {
                AdditionalInfo = currentCartItem.AdditionalDetails,
                CartId = currentCart.Id,
                ProductId = null,
                CartType = CartTypes.Shop,
                ReferenceId = payload
            };
            await _cartService.updateAdditionalInfo(req, userId, SiteId, currentCart.Id);
        }
        //Helpers 
        private async Task SendSmsAsync(string phoneNumber, string inspectionLink , string title)
        {
		
			long siteId = await _workContext.GetCurrentSiteIdAsync()
                .WaitAsync(TimeSpan.FromSeconds(10));

            var smsTemplateId = await GetSmsTemplateId(siteId , "FICMotorTemp", "MotorTemplate", "MotorTemplateAr");

            var parameters = new List<EmailParamsVM>
                {
                    new EmailParamsVM { Param = "[InspectionLink]", Value = inspectionLink }
                };

            var notificationObject = new NotificationObject
            {
                MessageBody = string.Empty,
                MessageTitle =title ,
                SendVia = SendBy.MobileNumber,
                SendValue = new List<string> { phoneNumber },
                NotificationType = NotificationType.SMS,
                EmailOptions = new EmailSenderVM
                {
                    Emails = new List<string>(),
                    TemplateId = smsTemplateId,
                    Params = parameters
                }
            };

            try
            {
                await _testNotifier.SendMessageAsync(NotificationType.SMS, null, notificationObject, siteId)
                    .WaitAsync(TimeSpan.FromMinutes(1));

            }
            catch (Exception ex)
            {

                throw;
            }
        }
        private async Task<string> GetSmsTemplateId(long siteId , string module , string arTemplate , string enTemplate )
        {

			var CultureId = _workContext.GetCurrentCultureId();
			var Culture = _siteCultureService.GetCultureById(CultureId);

			var settings = await _siteSettings.GetSiteSettingsByModule(module, siteId)
                .WaitAsync(TimeSpan.FromSeconds(10));
            try
			{
				var smsId = Culture.Culture.Contains("en")
				   ? GetStringSettingValue(settings, enTemplate)
				   : GetStringSettingValue(settings, arTemplate);

                return smsId;
			} catch {
                var smsId =
                    GetStringSettingValue(settings, enTemplate);
				return smsId;
			}
	 

			
        }
        private string GetStringSettingValue(List<SiteSettingsVM> settings, string settingId)
        {
            var s = settings.FirstOrDefault(x => x.Id == settingId);
            return s?.Value ?? string.Empty;
        }
        private async Task UploadAttachmentAsync(string userId, PdfFile pdfFile , JObject additionalDetails)
        {
			//var form = new MultipartFormDataContent();
			//form.Add(new ByteArrayContent(pdfFile.FileBytes), "file", pdfFile.Name);
			//form.Add(new StringContent(userId), "policyId");

			var payload = new AttachmentRequest
			{
				GeneralAttachment = new GeneralAttachment
				{
					CreatedBy = Settings.DefaultUser,
					DocumentType =    "178",  // firstFile.documentType,
					Path = pdfFile.Name,
					PolicyNo = additionalDetails.GetStringSafe("PolicyID", "DocumentID", "documentId", "GeneralQuotation.PolicyID", "insertquotationwrapper.GeneralQuotation.DocumentID"), // documentId,
					ReceivedDate = DateTime.Now.ToString(),
					Remarks = "Remarks",
					Serial = "2",
					Id = "0",
					AttachmentBytes = Convert.ToBase64String(pdfFile.FileBytes),
				}
			};


			var UploadAttachmentUrl = _carseerSettings.UploadAttachmentUrl;

             await PostAsync(UploadAttachmentUrl, payload);
            //response.EnsureSuccessStatusCode();
        }
        private static PdfFile ConvertToPdf(long userId, string overallStatus, string approvalResult)
        {
            var template = @"
                                <html>
                                <body style='font-family: Arial, sans-serif; padding: 30px;'>
                                    <h2>Vehicle Assessment Report</h2>
                                    <hr/>
                                    <p><strong>User ID:</strong> {userId}</p>
                                    <p><strong>Overall Vehicle Condition:</strong> {OverallStatus}</p>
                                    <p><strong>Approval Result:</strong> {ApprovalResult}</p>
                                    <p><strong>Date:</strong> {Date}</p>
                                </body>
                                </html>";

            var data = new
            {
                UserId = userId,
                OverallStatus = overallStatus,
                ApprovalResult = approvalResult,
                Date = DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")
            };

            return PdfHelper.Generate(template, data, $"assessment_{userId}");
        }


    }
}
