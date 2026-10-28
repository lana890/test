using System.ComponentModel.DataAnnotations;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.OTP.Models
{
    public class SendCodeViewModel
    {
        public SendCodeViewModel()
        {
            SMSProvider = SMSProviders.SMSZainJordan;
            WhatsAppProvider = WhatsAppProviders.Infoseed;
        }
        public E2FAProviders SelectedProvider { get; set; }
        public SMSProviders? SMSProvider { get; set; }
        public WhatsAppProviders? WhatsAppProvider { get; set; }

        public string Username { get; set; }

        public string Value { get; set; }
        public string Url { get; set; }
        public string EmailTemplateId { get; set; } = null;

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Purpose { get; set; }
        public bool ValidateUser { get; set; } = false;

        public Dictionary<string, string> AdditionalInfo { get; set; }
    }

    public class VerifyCodeViewModel
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        public string Code { get; set; }
        public string Username { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public string Purpose { get; set; }
    }

    public class UserOTPVM
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public List<UserOTPDetailsVM> lstOtps { get; set; }
    }

    public class UserOTPDetailsVM
    {
        public string Value { get; set; }
        public E2FAProviders ProviderType { get; set; }
        public string ProviderValue { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public string Purpose { get; set; }
        public TimeSpan Validity { get; set; }
        public DateTimeOffset ExpiryDate { get; set; }
    }

    public class EmailTemplateVM
    {
        public string Id { get; set; }
        public long SiteId { get; set; }
        public string CultureId { get; set; }
        public string Subject { get; set; }
        public long EmailTemplateTypeId { get; set; }
        public bool IsHTML { get; set; }
        public string EmailBody { get; set; }
        public string Color { get; set; }
        public string Icon { get; set; }
        public string AdditionalInfo { get; set; }
    }

    public class SiteSettingsVM
    {
        public string Id { get; set; }
        public string Value { get; set; }
        public string Module { get; set; }
        public bool IsVisibleInCommonSettingPage { get; set; }
        public long SiteId { get; set; }
    }

    public class ApiLogVM
    {
        public string ApiUrl { get; set; }
        public string FullUrl { get; set; }
        public string HttpMethod { get; set; }
        public long StatusCode { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }
        public long? SiteId { get; set; }
        public long? UserId { get; set; }
    }

    public class KeyValuePair
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class EmailParamsVM
    {
        public string Param { get; set; }
        public string Value { get; set; }
    }

    public class EmailSenderVM
    {
        public EmailSenderVM()
        {
            Attachment = new List<object>();
            Params = new List<EmailParamsVM>();
            Emails = new List<string>();
        }
        public string SenderModule { get; set; }
        public string TemplateId { get; set; }
        public List<EmailParamsVM> Params { get; set; }
        public List<object> Attachment { get; set; }
        public List<string> Emails { get; set; }
    }

    public enum SendBy
    {
        MobileNumber = 1,
        Email = 2,
        Username = 3,
        UserId = 4
    }

    public enum DesitinationType
    {
        Android,
        IOS,
        Personal
    }

    public class PushNotification
    {
        public PushNotification()
        {
            DesitinationType = DesitinationType.Personal;
            Topics = new List<string>();
        }
        public List<string> Topics { get; set; }
        public DesitinationType DesitinationType { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }

    public class NotificationObject
    {
        public NotificationObject()
        {
            MessageTitle = string.Empty;
            Severity = EskaCMS.Entities.Entity.NotificationSeverity.Success;
            PushNotificationOptions = new PushNotification();
            EmailOptions = new EmailSenderVM();
            SendValue = new List<string>();
            SendVia = new SendBy();
            Attachment = new List<object>();
        }
        public string MessageBody { get; set; }
        public string MessageTitle { get; set; }
        public PushNotification PushNotificationOptions { get; set; }
        public SendBy SendVia { get; set; }
        public List<string> SendValue { get; set; }
        public List<string> CC { get; set; } = new List<string>();
        public EskaCMS.OTP.Enums.NotificationType NotificationType { get; set; }

        public EskaCMS.Entities.Entity.NotificationSeverity Severity { get; set; }

        public long[] UserId { get; set; }

        public List<object> OnlineUsers { get; set; }

        public List<object> Severities { get; set; }

        public SMSProviders? SMSProvider { get; set; }
        public WhatsAppProviders? WhatsAppProvider { get; set; }

        public EmailSenderVM EmailOptions { get; set; }
        public List<object> Attachment { get; set; }
        public List<object> Files { get; set; }
        public string EmailTemplateId { get; set; }

        public bool isHTML { get; set; }

        public Dictionary<string, List<EmailParamsVM>> UserInfoWithParams { get; set; }
        public bool IsPdf { get; set; }
        public string PdfLink { get; set; }
        public string UserName { get; set; }
    }


    public class SMSZainconfig
    {
        public string sender_id { get; set; }
        public string ContentType { get; set; }
        public string URL { get; set; }
        public string IntegrationTokenURL { get; set; }
        public string IntegrationToken { get; set; }
        public string username { get; set; }
        public string password { get; set; }
    }

    public class SMSMPPconfig
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Source_Number { get; set; }
        public string Type { get; set; }
        public string URL { get; set; }
    }

    public class SMSconfig
    {
        public string service_type { get; set; }
        public string recipient_numbers_type { get; set; }
        public string content { get; set; }
        public string sender_id { get; set; }
        public string ContentType { get; set; }
        public string URL { get; set; }
        public string APIKey { get; set; }
        public string SenderId { get; set; }
    }

    public class SMSJorMallconfig
    {
        public string service_type { get; set; }
        public string recipient_numbers_type { get; set; }
        public string content { get; set; }
        public string sender_id { get; set; }
        public string ContentType { get; set; }
        public string URL { get; set; }
        public string APIKey { get; set; }
        public string SenderId { get; set; }
        public string AccName { get; set; }
        public string AccPass { get; set; }
        public long requesttimeout { get; set; }
    }

    public class UniFonicSMSVM
    {
        public string ApiURL { get; set; }
        public string Sender { get; set; }
        public string AppsId { get; set; }
        public string Encoding { get; set; }
    }

    public class MashastraSMSVM : UniFonicSMSVM
    {
        public string SMSType { get; set; }
        public string CountryCode { get; set; }
        public string SMSUserId { get; set; }
        public string pwd { get; set; }
    }

    public class SMSGlobalSettings
    {
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public string method { get; set; }
        public string host { get; set; }
        public string port { get; set; }
        public string version { get; set; }
        public string path { get; set; }
        public string smsid { get; set; }
        public string origin { get; set; }
    }

    public class SMSGlobalVM
    {
        public string destination { get; set; }
        public string message { get; set; }
        public string origin { get; set; }
    }

    public class InfoseedConfig
    {
        public string ApiUrl { get; set; } = "https://infoseedintegrationserverprod.azurewebsites.net/api/SendCampaign/SendMessage";
        public string Tenancy { get; set; } = "AlNejmahSweets";
        public string Username { get; set; }
        public string Password { get; set; }
        public string TemplateName { get; set; } = "text";
        public string DefaultRecipientName { get; set; }
    }

    public class WhatsAppMessageRequest
    {
        [Newtonsoft.Json.JsonProperty("reciverPhoneNumber")]
        public string ReciverPhoneNumber { get; set; }

        [Newtonsoft.Json.JsonProperty("reciverName")]
        public string ReciverName { get; set; }

        [Newtonsoft.Json.JsonProperty("mssageContent")]
        public string MssageContent { get; set; }

        [Newtonsoft.Json.JsonProperty("isPDF")]
        public bool IsPDF { get; set; }

        [Newtonsoft.Json.JsonProperty("linkPDF")]
        public string LinkPDF { get; set; }
    }
}
