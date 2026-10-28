using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Notifications;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.OTP.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.OTP.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(List<string> Emails, string SenderModule, string subject, string body, long SiteId,
            EmailsSettings emailSettings, bool isHtml = true, List<string> CC = null);
    }

    public class EmailSender : IEmailSender
    {
        private readonly string _assembly;
        private readonly IdcmsLogService _dcmsLogService;
        private readonly IConfiguration _config;
        private readonly ISiteSettingsService _siteSettingsService;

        public EmailSender(IConfiguration config, IdcmsLogService dcmsLogService, ISiteSettingsService siteSettingsService)
        {
            _dcmsLogService = dcmsLogService;
            _assembly = "EskaCMS.EmailSender.SMTP";
            _siteSettingsService = siteSettingsService;
            _config = config;
        }

        public async Task SendEmailAsync(
            List<string> Emails,
            string SenderModule,
            string subject,
            string body,
            long SiteId,
            EmailsSettings emailSettings,
            bool isHtml = true,
            List<string> CC = null)
        {
            try
            {
                if (emailSettings == null)
                {
                    emailSettings = await _siteSettingsService.GetEmailSetting(SiteId);
                }

                var message = new MimeMessage();
                var builder = new BodyBuilder();

                message.From.Add(MailboxAddress.Parse(emailSettings.SmtpSenderEmail));

                foreach (var email in Emails)
                {
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        message.To.Add(MailboxAddress.Parse(email));
                    }
                }

                if (CC != null && CC.Any())
                {
                    foreach (var ccEmail in CC)
                    {
                        if (!string.IsNullOrWhiteSpace(ccEmail))
                        {
                            message.Cc.Add(MailboxAddress.Parse(ccEmail));
                        }
                    }
                }

                message.Subject = subject;

                if (isHtml)
                    builder.HtmlBody = body;
                else
                    builder.TextBody = body;

                message.Body = builder.ToMessageBody();

                await Send(emailSettings, message);
            }
            catch (Exception exc)
            {
                await _dcmsLogService.AddError(
                    _assembly,
                    exc.Message,
                    "Failed to Send email: " + JsonConvert.SerializeObject(Emails),
                    exc.Message,
                    null);

                throw;
            }
        }

        private async Task Send(EmailsSettings emailSettings, MimeMessage message)
        {
            using (var client = await CreateConnectedClientAsync(emailSettings))
            {
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }

        private async Task<SmtpClient> CreateConnectedClientAsync(EmailsSettings emailSettings)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            var client = new SmtpClient();

            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            var secureSocketOptions = (SecureSocketOptions)_config.GetValue<int>("SmtpSecureSocketOptions");
            await client.ConnectAsync(emailSettings.SmtpServer, emailSettings.SmtpPort, secureSocketOptions);

            client.AuthenticationMechanisms.Remove("XOAUTH2");

            if (emailSettings.RequiresAuthentication)
                await client.AuthenticateAsync(emailSettings.SmtpUsername, emailSettings.SmtpPassword);

            return client;
        }
    }

    public interface ISmsSender
    {
        Task SendSmsAsync(SMSProviders SMSProvider, string PhoneNumber, string Message, long SiteId, string AdditionInfo = "");
    }

    public class SmsSender : ISmsSender
    {
        private readonly ISiteSettingsService _settingservice;
        private readonly IHttpClientFactory _httpClientFactory;

        public SmsSender(ISiteSettingsService settingservice, IHttpClientFactory httpClientFactory)
        {
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
                        await ZainSendSmsAsync(PhoneNumber, Message, SiteId);
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

        private async Task SendSmsUniFonicAsync(string number, string message, long SiteId)
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

                string ApiKey = ObjSettings.ApiKey;
                string ApiSecret = ObjSettings.ApiSecret;

                string method = ObjSettings.method;
                string host = ObjSettings.host;
                string port = ObjSettings.port;
                string version = ObjSettings.version;
                string path = ObjSettings.path;
                string extradata = string.Empty;
                Uri uri = new Uri(string.Format("https://{0}/{1}/{2}", host, version, path));

                string timestamp = ((int)(DateTime.UtcNow.AddSeconds(-60).Subtract(new DateTime(1970, 1, 1))).TotalSeconds).ToString();
                string nonce = Guid.NewGuid().ToString("N");
                string mac = string.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n", timestamp, nonce, method, uri.PathAndQuery, uri.Host, port, extradata);
                mac = Convert.ToBase64String((new HMACSHA256(Encoding.ASCII.GetBytes(ApiSecret))).ComputeHash(Encoding.ASCII.GetBytes(mac)));

                string authToken = string.Format("id=\"{0}\", ts=\"{1}\", nonce=\"{2}\", mac=\"{3}\"", ApiKey, timestamp, nonce, mac);

                SMSGlobalVM objtoken = new SMSGlobalVM();
                objtoken.destination = Destination;
                objtoken.message = Message;
                objtoken.origin = ObjSettings.origin;
                string result = string.Empty;
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

        private async Task SendSmsMashastraAsync(string number, string message, long SiteId)
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
                    var res = Enum.ToObject(typeof(SmartVisionReposneCodes), Convert.ToInt32(result));
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        #endregion
    }

    public interface IWhatsAppServices
    {
        Task SendMessageAsync(WhatsAppProviders WhatsAppProvider, string PhoneNumber, string ReciverName, bool IsPDF, string PdfLink, string Message, long SiteId, string AdditionInfo = "");
    }

    public class WhatsAppServices : IWhatsAppServices
    {
        private readonly ISiteSettingsService settingservice;
        private readonly IHttpClientFactory _httpClientFactory;

        public WhatsAppServices(ISiteSettingsService settingservice, IHttpClientFactory httpClientFactory)
        {
            this.settingservice = settingservice;
            _httpClientFactory = httpClientFactory;
        }

        public async Task SendMessageAsync(WhatsAppProviders WhatsAppProvider, string PhoneNumber, string ReciverName, bool IsPDF, string PdfLink, string Message, long SiteId, string AdditionInfo = "")
        {
            try
            {
                switch (WhatsAppProvider)
                {
                    case WhatsAppProviders.Infoseed:
                        await InfoseedSendAsync(PhoneNumber, ReciverName, IsPDF, PdfLink, Message, SiteId);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task InfoseedSendAsync(string phoneNumber, string ReciverName, bool IsPDF, string PdfLink, string message, long siteId)
        {
            try
            {
                var settings = await settingservice.GetSiteSettingById(WhatsAppProviders.Infoseed.ToString(), siteId);

                if (settings == null)
                {
                    throw new Exception("The Provider not Exist");
                }

                var smsSetting = JsonConvert.DeserializeObject<InfoseedConfig>(settings.Value);

                var request = new WhatsAppMessageRequest
                {
                    ReciverPhoneNumber = phoneNumber,
                    ReciverName = smsSetting.DefaultRecipientName ?? "Customer",
                    MssageContent = message,
                    IsPDF = IsPDF,
                    LinkPDF = IsPDF ? PdfLink : null
                };

                using (HttpClient httpClient = _httpClientFactory.CreateClient())
                {
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                    httpClient.DefaultRequestHeaders.Add("Tenancy", smsSetting.Tenancy);
                    httpClient.DefaultRequestHeaders.Add("Username", smsSetting.Username);
                    httpClient.DefaultRequestHeaders.Add("Password", smsSetting.Password);
                    httpClient.DefaultRequestHeaders.Add("TemplateName", smsSetting.TemplateName);
                    var jsonContent = JsonConvert.SerializeObject(request);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json-patch+json");

                    HttpResponseMessage response = await httpClient.PostAsync(smsSetting.ApiUrl, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Failed to send the message. Status code: {response.StatusCode} and Content: {responseContent}");
                    }
                    else
                    {
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
