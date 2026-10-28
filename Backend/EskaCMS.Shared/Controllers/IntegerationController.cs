using EskaCMS.ADIntegration.Interfaces;
using EskaCMS.ADIntegration.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels.Account;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Security.Services.Interfaces;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Models;
using EskaCommerce.Notifications.Models.NotificationModels;
using EskaCommerce.Notifications.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.ADIntegration.Controllers
{
    [Route("api/Integeration")]
    [ApiController]
    [Authorize]
    public class IntegerationController : ControllerBase
    {
        private readonly IWorkContext _workContext;
        //private readonly IIntegerationService _integrationService;
        private readonly IUserOTPService _UserOTPService;
        private readonly IApiMapper _ApiMapperService;
        private readonly INotificationPublisher _notificationPublisher;
        // private readonly IConfiguration _configuration;
        private readonly IRepositoryWithTypedId<SiteSettings, string> _appSettingRepository;
        private readonly IHttpClientFactory _httpClientFactory;


        public IntegerationController(IWorkContext workContext,
                  //, IIntegerationService integrationService
                  IUserOTPService UserOTPService,
                  IApiMapper ApiMapperService,
                  INotificationPublisher notificationPublisher,
                  /* IConfiguration configuration, */
            IRepositoryWithTypedId<SiteSettings, string> appSettingRepository,
            IHttpClientFactory httpClientFactory
            )
        {
            _workContext = workContext;
            //_integrationService = integrationService;
            _UserOTPService = UserOTPService;
            // _configuration = configuration;
            _notificationPublisher = notificationPublisher;
            _appSettingRepository = appSettingRepository;
            _ApiMapperService = ApiMapperService;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost]
        [Route("SendCode")]
        [Authorize]
        public async Task<IActionResult> SendCode([FromBody] SendCodeIntegerationViewModel model)
        {
            try
            {
                long SiteId = _workContext.GetCurrentSiteId();
                User user = new User();
                if (SiteId == 0)
                    throw new Exception("SiteId cannot be null");
                user = await _workContext.GetCurrentUser();

                if (user == null)
                {
                    throw new Exception("Something went wrong!");
                }

                string code = string.Empty;

                if (model.IntegerationType == EIntegerationType.FTTH)
                {
                    code = await handleFTTHRequest(model, user);
                }
                else if (model.IntegerationType == EIntegerationType.LTE)
                {
                    code = await handleLTERequest(model);
                }

                if (!string.IsNullOrEmpty(code))
                {
                    var sendCodeViewModel = new SendCodeViewModel
                    {
                        Value = model.Value,
                        SelectedProvider = model.SelectedProvider,
                        Username = model.Username,
                        Purpose = model.Purpose
                    };

                    await _UserOTPService.AddUserOTPAsync(user.Id, SiteId, sendCodeViewModel, code);


                    await _notificationPublisher.PublishAsync(
                          NotificationType.Integeration.ToString(),
                          new MessageNotificationData($"Integeration Code Is {code}", "Absher Integeration", String.Join(",", model.Value)),
                          SiteId: SiteId,
                          severity: NotificationSeverity.Success,
                          userIds: new[] { user.Id }

                          );

                    return Ok();
                }
                else
                {
                    return BadRequest("National Id Was Not Found In Absher, If You Are Sure Your Phone Number Is Connected To Absher Please Try To Contact GO Customer Service.");
                }
                //}
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        private async Task<string> handleFTTHRequest(SendCodeIntegerationViewModel model, User user)
        {
            //var apiData = await _ApiMapperService.GetApiMapperById(model.InvokerId);

            //JObject parentObj = new JObject();
            //parentObj["USERNAME"] = "ADMIN";
            //parentObj["PASSWORD"] = "Eska@1234";
            //parentObj["COMPANYID"] = 1;
            //parentObj["BRANCHID"] = 1;

            //var parentRes = await _ApiMapperService.HandleWebAPIRequests(apiData.ParentId.Value, parentObj, user.Id);

            //if (parentRes == null)
            //{
            //    throw new Exception("Something Went Wrong Try To Resend Code Or Contact Administrators -- Parent");
            //}




            //var token = parentRes;

            //if (token == null)
            //{
            //    throw new Exception("Something Went Wrong Try To Resend Code Or Contact Administrators -- Token");
            //}

            JObject apiObj = new JObject();
            apiObj["WorkflowInstanceId"] = model.WorkFlowId;
            apiObj["eventCode"] = "absotpv2";

            var apiResult = await _ApiMapperService.HandleWebAPIRequests(model.InvokerId, apiObj, user.Id);

            if (apiResult == null)
            {
                throw new Exception("Something Went Wrong Try To Resend Code Or Contact Administrators");
            }

            if (apiResult.data == null)
            {
                throw new Exception("National Id Was Not Found In Absher, If You Are Sure Your Phone Number Is Connected To Absher Please Try To Contact GO Customer Service.");
            }
            var code = apiResult.data.ToString();
            return code;
        }

        private async Task<string> handleLTERequest(SendCodeIntegerationViewModel model)
        {
            string code = string.Empty;
            var parameters = new
            {
                ClientID = "7001580625",
                ClientAuthorization = "ltf4SR2JMR1wKwFVf3k6AbZIcOSatNslSVrFbIdKBMM=",
                OperatorID = "2731010263",
                CustomerID = model.Username,
                LanguagePref = "en",
                Reason = "New Consumer Account Order"
            };

            var obj = new
            {
                source = "Customer Care",
                requestId = Guid.NewGuid().ToString(),
                createDate = new DateTimeOffset(DateTimeOffset.Now.DateTime, TimeSpan.Zero).ToString(),
                data = new
                {
                    eventCode = "ABSOTPV2",
                    isBatch = false,
                    priority = 1,
                    bccCustomerNumber = 18996258,
                    bccSubProductsId = 5699,
                    bccContractNumber = model.ContractId ?? 100002948,
                    workFlowID = model.WorkFlowId ?? 13715,
                    parameters = new[] { parameters }
                }
            };

            using var client = _httpClientFactory.CreateClient();
            var URL = _appSettingRepository.Query().Where(x => x.Id == "AbsherConfig").Select(x => x.Value).FirstOrDefault();
            if (URL == null)
            {
                URL = "http://172.16.69.108:9090/EskaAPI/ServiceActivator/OnlineTransaction";
            }

            var apiUri = new System.Uri(URL);
            client.Timeout = TimeSpan.FromSeconds(50);
            var sp = ServicePointManager.FindServicePoint(apiUri);
            sp.ConnectionLeaseTimeout = 50000;
            sp.MaxIdleTime = 50000;
            sp.ConnectionLimit = 50000;
            ServicePointManager.DefaultConnectionLimit = 200;
            var content = new StringContent(JsonConvert.SerializeObject(obj), System.Text.Encoding.UTF8, "application/json");

            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = new HttpMethod("POST"),
                RequestUri = apiUri,
                Content = content
            };
            using var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                dynamic apiResult = JsonConvert.DeserializeObject(result);

                if (apiResult == null || apiResult.status != 200)
                {
                    throw new Exception("Something Went Wrong Try To Resend Code Or Contact Administrators");
                }

                if (apiResult.data == null)
                {
                    throw new Exception("Something Went Wrong Try To Resend Code Or Contact Administrators");
                }

                code = apiResult.data.verificationCode.ToString();
            }
            else
            {
                //var test = result.ReasonPhrase;
                throw new Exception("National Id Was Not Found In Absher, If You Are Sure Your Phone Number Is Connected To Absher Please Try To Contact GO Customer Service.");
            }
            return code;
        }
    }
}
