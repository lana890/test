using Azure;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Services
{
    public class SmsSender : ISmsSender
    {
        private readonly IRepositoryWithTypedId<SiteSettings, string> _appSettingRepository;

        private readonly ISiteSettingsService _settingservice;
        private readonly IHttpClientFactory _httpClientFactory;
        public SmsSender(IRepositoryWithTypedId<SiteSettings, string> appSettingRepository,ISiteSettingsService settingservice, IHttpClientFactory httpClientFactory)
        {
            _appSettingRepository = appSettingRepository;
            _settingservice = settingservice;
            _httpClientFactory = httpClientFactory;
        }
        public async Task SendSmsAsync(SMSProviders SMSProvider, string PhoneNumber, string Message, long SiteId, string AdditionInfo = "")
        {
            try
            {
                switch (SMSProvider)
                {
                    case SMSProviders.SMSZainJordan:
                        await ZainSendSmsAsync(PhoneNumber,  Message, SiteId);
                        break;
                    case SMSProviders.SMSGlobal:
                        await GlobalSMSSendSMSAsync(PhoneNumber, Message, SiteId);
                        break;
                    case SMSProviders.SMSUniFonic:
                        await SendSmsUniFonicAsync(PhoneNumber, Message, SiteId);
                        break;
                    case SMSProviders.SMSVision:
                        await SMSVisionSendSmsAsync(PhoneNumber, Message, SiteId);
                        break;
                    case SMSProviders.SMSJorMall:
                        await JorMallSendSmsAsync(PhoneNumber, Message, SiteId);
                        break;

                    case SMSProviders.SMSMashastra:
                        await SendSmsMashastraAsync(PhoneNumber, Message, SiteId);
                        break;
                    case SMSProviders.SMSMPP:
                        await SMSMPPSendSmsAsync(PhoneNumber, Message, SiteId);
                        break;
                    default:
                        break;
                }



            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        #region SMS Provider
        private async Task ZainSendSmsAsync(string mobile, string message, long siteId)
        {
            try
            {
               
                var setting = await _settingservice.GetSiteSettingById(SMSProviders.SMSZainJordan.ToString(), siteId);

                var smsSetting = JsonConvert.DeserializeObject<SMSZainconfig>(setting.Value);


                var smsPayload = new
                {
                    service_type = "bulk_sms",
                    recipient_numbers_type = "single_numbers",
                    phone_numbers = new[] { mobile },
                    content = message,
                    sender_id = smsSetting.sender_id
                };

                using (HttpClient httpClient = _httpClientFactory.CreateClient())
                {
                    if (string.IsNullOrEmpty(smsSetting.IntegrationToken))

                    {
                        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, smsSetting.IntegrationTokenURL);
                        tokenRequest.Headers.Add("username", smsSetting.username);
                        tokenRequest.Headers.Add("password", smsSetting.password);

                        var tokenResponse = await httpClient.SendAsync(tokenRequest);
                        tokenResponse.EnsureSuccessStatusCode();

                        var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
                        var tokenJson = JObject.Parse(tokenContent);
                        string integrationToken = tokenJson["result"]?["integration_token"]?.ToString();
                        smsSetting.IntegrationToken = integrationToken;
                        if (string.IsNullOrEmpty(integrationToken))
                            throw new Exception("Failed to retrieve integration token.");
                    }
                    using var smsRequest = new HttpRequestMessage(HttpMethod.Post, smsSetting.URL);
                    var hh = JsonConvert.SerializeObject(smsPayload);
                    smsRequest.Headers.Add("integration_token", smsSetting.IntegrationToken);
                    smsRequest.Content = new StringContent(JsonConvert.SerializeObject(smsPayload), Encoding.UTF8, "application/json");

                    var smsResponse = await httpClient.SendAsync(smsRequest);
                    smsResponse.EnsureSuccessStatusCode();

                    var smsResult = await smsResponse.Content.ReadAsStringAsync();
                    var smsResultJson = JObject.Parse(smsResult);

                    string status = smsResultJson["status"]?.ToString();

                    if (status == "saved successfully!")
                    {
                        Console.WriteLine("SMS sent successfully.");
                    }
                    else
                    {
                        throw new Exception($"Failed to send the message.");
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task SMSMPPSendSmsAsync(string phoneNumber, string message, long siteId)
        {
            try
            {
                var setting = await _settingservice.GetSiteSettingById(SMSProviders.SMSMPP.ToString(), siteId);

                if (setting == null)
                {
                    throw new Exception("The Provider not Exist");
                
                }
                var SMSSetting = JsonConvert.DeserializeObject<SMSMPPconfig>(setting.Value);

                long Type = await DetermineTextType(message);

                var Url = @$"{SMSSetting.URL}user={SMSSetting.UserName}&pass={SMSSetting.Password}&sid={SMSSetting.Source_Number}&mno={phoneNumber}&text={message}";

                using (HttpClient httpClient = _httpClientFactory.CreateClient())
                {
                    HttpResponseMessage response = await httpClient.GetAsync(Url);

                    if (response.IsSuccessStatusCode)
                    {
                        await response.Content.ReadAsStringAsync();
                        return;
                    }
                    else
                    {
                        throw new Exception($"Failed to send the message. Status code: {response.StatusCode} and Content:{response.Content}");
                    }
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private async Task<long> DetermineTextType(string message)
        {
            try
            {
                message = Regex.Unescape(message);

                bool containsSpecialCharacters = message.Any(s => !Char.IsLetterOrDigit(s) && !Char.IsWhiteSpace(s));

                bool containsArabic = message.Any(f => f >= 0x0600 && f <= 0x06FF);
                
                bool containsEnglish = message.Any(e => (e >= 'A' && e <= 'Z') || (e >= 'a' && e <= 'z'));
                
                bool containsUnicode = message.Any(c => c > 127);


                if (containsEnglish)
                {
                    return 1;
                }
                else if (containsArabic)
                {
                    return 4;
                }
               else if (containsSpecialCharacters)
                {
                    return 3; 
                }
                else if (containsUnicode)
                {
                    return 2; 
                }
                else
                {
                    return 1; //Defualt
                }
            
            }
            catch (Exception ex)
            {

                throw ex;
            }       
        
        }
        private async Task SMSVisionSendSmsAsync(string number, string message, long SiteId)
        {
            try
            {
                var settings = await _settingservice.GetSiteSettingById(SMSProviders.SMSVision.ToString(), SiteId);

                var SMSSetting = JsonConvert.DeserializeObject<SMSconfig>(settings.Value);
                //  string content = "New Online Payment Transaction was Registered with Voucher Number : " + "";
                var URL = SMSSetting.URL + "?" + "api_key=" + SMSSetting.APIKey + "&type=text" + "&contacts=" + number + "&senderid=" + SMSSetting.SenderId + "&msg=" + message;
                using var httpClient = _httpClientFactory.CreateClient();
                using var response = await httpClient.GetAsync(URL);
                string resultww = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    var res = Enum.ToObject(typeof(SmartVisionReposneCodes), Convert.ToInt32(result));

                }

            }
            catch (Exception e)
            {
                throw e;
            }


        }
        private async Task SendSmsUniFonicAsync(string number, string message, long SiteId) //Der3 (ASIC)
        {
            try
            {
                var settings = await _settingservice.GetSiteSettingById(SMSProviders.SMSUniFonic.ToString(), SiteId);
                if (settings == null)
                    throw new Exception("No settings for such provider");

                var SMSSetting = JsonConvert.DeserializeObject<UniFonicSMSVM>(settings.Value);
                var URL = SMSSetting.ApiURL + "?" + "to=" + number + "&msg=" + message + "&sender=" + SMSSetting.Sender + "&appsid=" + SMSSetting.AppsId + "&encoding=" + SMSSetting.Encoding;
                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromMinutes(60);
                using var response = await httpClient.GetAsync(URL);
                string resultww = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    throw new Exception(result);
                }
                

            }
            catch (Exception e)
            {
               
                throw e;
            }


        }
        private async Task GlobalSMSSendSMSAsync(string Destination, string Message, long SiteId)
        {
            try
            {
                var settings = await _settingservice.GetSiteSettingById(SMSProviders.SMSGlobal.ToString(), SiteId);
                if (settings == null)
                    throw new Exception("No settings for such provider");

                var ObjSettings = JsonConvert.DeserializeObject<SMSGlobalSettings>(settings.Value);

                string ApiKey = ObjSettings.ApiKey;// "8c0e4c3e21a6607ed328ab2f1a253017";
                string ApiSecret = ObjSettings.ApiSecret;// "92575d6ce94643eb6356a2bdb17b4699";

                string method = ObjSettings.method;// "GET";
                string host = ObjSettings.host;// "api.smsglobal.com";
                string port = ObjSettings.port;// "443";
                string version = ObjSettings.version;// "v2";
                string path = ObjSettings.path;// "sms";
                //string smsid = ObjSettings.smsid;// "";
                string extradata = string.Empty;
                Uri uri = new Uri(string.Format("https://{0}/{1}/{2}", host, version, path));

                string timestamp = ((int)(DateTime.UtcNow.AddSeconds(-60).Subtract(new DateTime(1970, 1, 1))).TotalSeconds).ToString();
                string nonce = Guid.NewGuid().ToString("N");
                string mac = string.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n", timestamp, nonce, method, uri.PathAndQuery, uri.Host, port, extradata);
                mac = Convert.ToBase64String((new HMACSHA256(Encoding.ASCII.GetBytes(ApiSecret))).ComputeHash(Encoding.ASCII.GetBytes(mac)));

                string authToken = string.Format("id=\"{0}\", ts=\"{1}\", nonce=\"{2}\", mac=\"{3}\"", ApiKey, timestamp, nonce, mac);

                SMSGlobalVM objtoken = new SMSGlobalVM();
                objtoken.destination = Destination;// "971509190910";
                objtoken.message = Message;//.Add(  "TestHomeCuts");
                objtoken.origin = ObjSettings.origin;// "Test";
                string result = string.Empty;
                JObject jsonResult;
                string Json = JsonConvert.SerializeObject(objtoken);
                HttpContent Obj = new StringContent(Json, Encoding.UTF8, "application/json");
                using (var client = _httpClientFactory.CreateClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("MAC", authToken);
                    var Base = new Uri(string.Format("{0}://{1}/{2}/{3}", uri.Scheme, uri.Host, version, path));
                    HttpResponseMessage response = await client.PostAsync(Base, Obj);
                    result = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                    }
                }
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
        private async Task SendSmsMashastraAsync(string number, string message, long SiteId) //Der3 (ASIC)
        {
            try
            {
                var settings = await _settingservice.GetSiteSettingById(SMSProviders.SMSMashastra.ToString(), SiteId);
                if (settings == null)
                    throw new Exception("No settings for such provider");

                var SMSSetting = JsonConvert.DeserializeObject<MashastraSMSVM>(settings.Value);
                var URL = SMSSetting.ApiURL + "?" + "user=" + SMSSetting.SMSUserId + "&pwd=" + SMSSetting.pwd + "&senderid=" + SMSSetting.Sender + "&CountryCode=" + SMSSetting.CountryCode + "&mobileno=" + number.Remove(0, 1)
                    + "&msgtext=" + message + "&smstype=" + SMSSetting.SMSType;
                using var httpClient = _httpClientFactory.CreateClient();
                using var response = await httpClient.GetAsync(URL);
                string resultww = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                }

            }
            catch (Exception e)
            {
                throw e;
            }


        }
        private async Task JorMallSendSmsAsync(string number, string message, long SiteId)
        {
            try
            {
                var settings = await _settingservice.GetSiteSettingById(SMSProviders.SMSJorMall.ToString(), SiteId);

                var SMSSetting = JsonConvert.DeserializeObject<SMSJorMallconfig>(settings.Value);
                //  string content = "New Online Payment Transaction was Registered with Voucher Number : " + "";
                var URL = SMSSetting.URL + "?"
                + "senderid=" + SMSSetting.SenderId
                + "&numbers=" + number
                + "&accname=" + SMSSetting.AccName
                + "&accpass=" + SMSSetting.AccPass
                + "&msg=" + HttpUtility.UrlEncode(message)
                + "&id=";

                using var httpClient = _httpClientFactory.CreateClient();
                using var response = await httpClient.GetAsync(URL);
                if (!response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    var res = Enum.ToObject(typeof(GeneralEnums.SmartVisionReposneCodes), Convert.ToInt32(result));

                }

            }
            catch (Exception e)
            {
                throw e;
            }


        }
        #endregion

    }
}
