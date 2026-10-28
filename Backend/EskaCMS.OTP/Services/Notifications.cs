using System.Text.RegularExpressions;
using EskaCMS.OTP.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Notifications;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.OTP.Enums;
using EskaCMS.OTP.Infrastructure;
using EskaCMS.OTP.Models;
using EskaCommerce.Notifications.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.OTP.Services
{
    public interface ITestNotifier
    {
        Task SendMessageAsync(NotificationType notificationType, EmailsSettings EmailSettings = null, NotificationObject data = null, long siteId = 0, long? UserId = null);
    }

    public class TestNotifier : ITestNotifier
    {
        private readonly INotificationPublisher _notificationPublisher;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;
        private readonly IRepository<User> _users;
        private readonly IWorkContext _workContext;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IWhatsAppServices whatsAppServices;
        private readonly IdcmsLogService _dcmsLogService;
        private readonly ISiteSettingsService _settingservice;

        public TestNotifier(INotificationPublisher notificationPublisher,
            IEmailSender emailSender,
            ISmsSender smsSender,
            IRepository<User> users,
            IEmailTemplateService emailTemplateService,
            IWhatsAppServices whatsAppServices,
            ISiteSettingsService settingservice,
            IWorkContext workContext,
            IdcmsLogService dcmsLogService)
        {
            _notificationPublisher = notificationPublisher;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _users = users;
            _workContext = workContext;
            _emailTemplateService = emailTemplateService;
            this.whatsAppServices = whatsAppServices;
            _dcmsLogService = dcmsLogService;
            _settingservice = settingservice;
        }

        public async Task SendMessageAsync(
            NotificationType notificationType,
            EmailsSettings EmailSettings = null,
            NotificationObject data = null,
            long siteId = 0,
            long? UserId = null)
        {
            try
            {
                List<long> lstOfUserIds = new List<long>();

                if (notificationType == NotificationType.SMS)
                {
                    if (data.EmailOptions != null && data.EmailOptions.Params != null)
                    {
                        data = await ReplaceTemplateParams(data, data.EmailOptions.TemplateId);
                        data.MessageBody = HtmlToPlainText(data.MessageBody);
                        lstOfUserIds = await SendSmsHelper(data, siteId);
                    }
                    else
                    {
                        lstOfUserIds = await SendSmsHelper(data, siteId);
                    }

                    data.UserId = lstOfUserIds.ToArray();
                }
                else if (notificationType == NotificationType.Email)
                {
                    var isHTML = true;

                    if (data.EmailOptions?.Params != null)
                        data = await ReplaceTemplateParams(data, data.EmailOptions.TemplateId);

                    await _dcmsLogService.AddInfo("Notification Module",
                        $"Sending Email To : {JsonConvert.SerializeObject(data.SendValue)}",
                        JsonConvert.SerializeObject(data), siteId);

                    await _emailSender.SendEmailAsync(data.SendValue, data.EmailOptions.SenderModule,
                        data.MessageTitle, data.MessageBody, siteId, EmailSettings, isHTML, data.CC);

                    await _dcmsLogService.AddInfo("Notification Module",
                        $"Successfully send Email To : {JsonConvert.SerializeObject(data.SendValue)}",
                        JsonConvert.SerializeObject(data), siteId);

                    foreach (string item in data.SendValue)
                    {
                        if (string.IsNullOrEmpty(item)) continue;
                        try
                        {
                            var normalized = item.ToUpper();
                            User user = await _users.Query()
                              .AsNoTracking()
                              .FirstOrDefaultAsync(x => x.NormalizedEmail == normalized);

                            if (user != null)
                            {
                                lstOfUserIds.Add(UserId ?? user.Id);
                            }
                            else
                            {
                                var currentUser = await _workContext.GetCurrentUser();
                                if (currentUser != null)
                                    lstOfUserIds.Add(currentUser.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            await _dcmsLogService.AddError("Notification Module", ex.Message,
                                "Could not resolve user for email notification", JsonConvert.SerializeObject(ex), siteId);
                        }
                    }

                    data.UserId = lstOfUserIds.ToArray();
                }
                else if (notificationType == NotificationType.WhatsApp)
                {
                    if (data.EmailOptions?.Params != null)
                    {
                        data = await ReplaceTemplateParams(data, data.EmailOptions.TemplateId);
                        data.MessageBody = HtmlToPlainText(data.MessageBody);
                        lstOfUserIds = await SendWhatAppHelper(data, siteId);
                    }
                    else
                    {
                        lstOfUserIds = await SendWhatAppHelper(data, siteId);
                    }

                    data.UserId = lstOfUserIds.ToArray();
                }

                if (data.UserId.Any())
                {
                    await _notificationPublisher.PublishAsync(
                        notificationType.ToString(),
                        new MessageNotificationData(
                            data.MessageBody,
                            data.MessageTitle,
                            string.Join(",", data.SendValue.Select(p => p.ToString()).ToArray()),
                            JsonConvert.SerializeObject(data.PushNotificationOptions.Data)),
                        SiteId: siteId,
                        severity: data.Severity,
                        userIds: data.UserId,
                        EmailTemplateId: data.EmailOptions.TemplateId ?? data.EmailTemplateId);
                }
            }
            catch (Exception ex)
            {
                await _dcmsLogService.AddError("Notification Module", ex.Message,
                    $"Error Send Notification To : {JsonConvert.SerializeObject(data.SendValue)}",
                    JsonConvert.SerializeObject(data), siteId);

                await _notificationPublisher.PublishAsync(
                    notificationType.ToString(),
                    new MessageNotificationData(data.MessageBody, data.MessageTitle, ex.Message),
                    SiteId: siteId,
                    severity: NotificationSeverity.Error,
                    userIds: data.UserId,
                    EmailTemplateId: data.EmailOptions.TemplateId ?? data.EmailTemplateId);

                throw;
            }
        }

        private static readonly Regex _lineBreakRegex = new Regex(@"<(br|BR)\s{0,1}\/{0,1}>", RegexOptions.Multiline);
        private static readonly Regex _stripFormattingRegex = new Regex(@"<[^>]*(>|$)", RegexOptions.Multiline);
        private static readonly Regex _tagWhiteSpaceRegex = new Regex(@"(>|$)(\W|\n|\r)+<", RegexOptions.Multiline);

        private string HtmlToPlainText(string html)
        {
            var text = html;
            text = System.Net.WebUtility.HtmlDecode(text);
            text = _tagWhiteSpaceRegex.Replace(text, "><");
            text = _lineBreakRegex.Replace(text, Environment.NewLine);
            text = _stripFormattingRegex.Replace(text, string.Empty);
            text = System.Net.WebUtility.HtmlDecode(text);

            return text;
        }

        public async Task<NotificationObject> ReplaceTemplateParams(NotificationObject data, string emailTemplateId)
        {
            var emailTemplate = await _emailTemplateService.GetEmailTemplateById(emailTemplateId);

            if (emailTemplate != null)
            {
                if (data.EmailOptions.Params.Count() > 0)
                {
                    foreach (var item in data.EmailOptions.Params)
                    {
                        emailTemplate.EmailBody = emailTemplate.EmailBody.Replace(item.Param, item.Value);
                    }

                    data.MessageTitle = string.IsNullOrEmpty(data.MessageTitle) ? emailTemplate.Subject : data.MessageTitle;
                    data.MessageBody = emailTemplate.EmailBody;
                    data.isHTML = true;
                }
                else if (string.IsNullOrEmpty(data.MessageBody) && string.IsNullOrEmpty(data.MessageTitle))
                {
                    data.MessageTitle = string.IsNullOrEmpty(data.MessageTitle) ? emailTemplate.Subject : data.MessageTitle;
                    data.MessageBody = emailTemplate.EmailBody;
                }
                else
                {
                    data.isHTML = emailTemplate.IsHTML;
                }
            }

            return data;
        }

        public async Task<List<long>> SendSmsHelper(NotificationObject data, long siteId)
        {
            List<long> lstOfUserIds = new List<long>();
            foreach (string value in data.SendValue)
            {
                var smsProvider = data.SMSProvider;
                if (smsProvider is null)
                {
                    var defaultSmsProvider = (await _settingservice.GetSiteSettingById("SmsProvider", siteId))?.Value;

                    if (defaultSmsProvider is null)
                        throw new Exception("Please add default provider");

                    smsProvider = (SMSProviders)Enum.Parse(typeof(SMSProviders), defaultSmsProvider, true);
                }
                await _dcmsLogService.AddInfo("Notification Module", $"Sending SMS To : {JsonConvert.SerializeObject(data.SendValue)}", JsonConvert.SerializeObject(data), siteId);

                try
                {
                    await _smsSender.SendSmsAsync(smsProvider.Value, value, data.MessageBody, siteId);
                }
                catch (Exception e)
                {
                    await _dcmsLogService.AddError("SMS Sender (Unifonic)", e.Message, "Cannot Send SMS for : " + value, data.MessageBody, siteId);
                    throw e;
                }

                await _dcmsLogService.AddInfo("Notification Module", $"Send  SMS Successfully To : {JsonConvert.SerializeObject(data.SendValue)}", JsonConvert.SerializeObject(data), siteId);
                var normalized = value.Trim();
                var user = await _users.Query()
                    .AsNoTracking()
                    .Where(x => x.PhoneNumber == normalized)
                    .Select(x => new { x.Id })
                    .FirstOrDefaultAsync();

                if (user != null)
                {
                    lstOfUserIds.Add(user.Id);
                }
                else
                {
                    try
                    {
                        var userId = await _workContext.GetCurrentUserId();
                        lstOfUserIds.Add(userId);
                    }
                    catch (Exception)
                    {
                        await _dcmsLogService.AddInfo("Notification", "No current user ", "Background Job", siteId);
                    }
                }
            }

            return lstOfUserIds;
        }

        public async Task<List<long>> SendWhatAppHelper(NotificationObject data, long siteId)
        {
            List<long> lstOfUserIds = new List<long>();
            foreach (string value in data.SendValue)
            {
                User user = await _users.Query().Where(x => x.UserName.ToLower().Contains(data.UserName)).FirstOrDefaultAsync();

                var WhatsAppProvider = data.WhatsAppProvider;
                if (WhatsAppProvider is null)
                {
                    var defaultWhatsAppProvider = (await _settingservice.GetSiteSettingById("WhatsAppProvider", siteId))?.Value;

                    if (defaultWhatsAppProvider is null)
                        throw new Exception("Please add default provider");

                    WhatsAppProvider = (WhatsAppProviders)Enum.Parse(typeof(WhatsAppProviders), defaultWhatsAppProvider, true);
                }
                await _dcmsLogService.AddInfo("Notification Module", $"Sending SMS To : {JsonConvert.SerializeObject(data.SendValue)}", JsonConvert.SerializeObject(data), siteId);

                try
                {
                    await whatsAppServices.SendMessageAsync(WhatsAppProvider.Value, value, user?.FullName ?? "Customer", data.IsPdf, data.PdfLink, data.MessageBody, siteId);
                }
                catch (Exception e)
                {
                    await _dcmsLogService.AddError("WhatsApp Sender", e.Message, "Cannot Send WhatsApp for : " + value, data.MessageBody, siteId);
                    throw e;
                }

                await _dcmsLogService.AddInfo("Notification Module", $"Send  WhatsApp Successfully To : {JsonConvert.SerializeObject(data.SendValue)}", JsonConvert.SerializeObject(data), siteId);
                if (user != null)
                {
                    lstOfUserIds.Add(user.Id);
                }
                else
                {
                    try
                    {
                        lstOfUserIds.Add(_workContext.GetCurrentUser().Result.Id);
                    }
                    catch (Exception)
                    {
                        await _dcmsLogService.AddInfo("Notification", "No current user ", "Background Job", siteId);
                    }
                }
            }

            return lstOfUserIds;
        }
    }

    public interface INotificationPublisher
    {
        Task PublishAsync(
            string notificationName,
            NotificationData data = null,
            long SiteId = 0,
            NotificationSeverity severity = NotificationSeverity.Info,
            long[] userIds = null,
            long[] excludedUserIds = null,
            string EmailTemplateId = null,
            long CurrentUserId = 0);
    }

    public class NotificationPublisher : INotificationPublisher
    {
        private readonly IRepository<NotificationScheme> _notificationSchemeRepository;
        private readonly INotificationDistributer _notificationDistributer;
        private readonly IWorkContext _workContext;
        private readonly IConfiguration _config;

        public NotificationPublisher(
            IRepository<NotificationScheme> notificationSchemeRepository,
            INotificationDistributer notificationDistributer,
            IWorkContext workContext,
            IConfiguration config)
        {
            _notificationSchemeRepository = notificationSchemeRepository;
            _notificationDistributer = notificationDistributer;
            _workContext = workContext;
            _config = config;
        }

        public virtual async Task PublishAsync(
            string notificationName,
            NotificationData data = null,
            long siteId = 0,
            NotificationSeverity severity = NotificationSeverity.Info,
            long[] userIds = null,
            long[] excludedUserIds = null,
            string emailTemplateId = null,
            long currentUserId = 0)
        {
            if (string.IsNullOrWhiteSpace(notificationName))
                throw new ArgumentException("NotificationName cannot be null or empty", nameof(notificationName));

            int chunkSize = _config.GetValue<int>("chuncks");

            if (chunkSize <= 0)
                chunkSize = 1000;

            if (currentUserId == 0)
                currentUserId = await _workContext.GetCurrentUserId();

            var finalUserIds = (userIds == null || userIds.Length == 0)
                ? new[] { currentUserId }
                : userIds;

            if (finalUserIds.All(id => id == 0))
                throw new InvalidOperationException("No valid user found for notification publishing");

            var schemeIds = new List<long>();

            for (int i = 0; i < finalUserIds.Length; i += chunkSize)
            {
                var userChunk = finalUserIds.Skip(i).Take(chunkSize).ToArray();

                var scheme = new NotificationScheme
                {
                    NotificationName = notificationName,
                    EntityTypeName = null,
                    EntityTypeAssemblyQualifiedName = null,
                    EntityId = null,
                    SiteId = siteId == 0 ? 1 : siteId,
                    Severity = severity,
                    IsRead = false,
                    UserIds = string.Join(",", userChunk),
                    ExcludedUserIds = excludedUserIds?.Any() == true ? string.Join(",", excludedUserIds) : "",
                    Data = data == null ? null : JsonConvert.SerializeObject(data, new JsonSerializerSettings()),
                    DataTypeName = data == null ? null : NotificationData.MainSystemMessageNotificationDataTypeName,
                    CreatedById = currentUserId,
                    EmailTemplateId = emailTemplateId,
                    EmailTemplateSiteId = siteId == 0 ? 1 : siteId
                };

                _notificationSchemeRepository.Add(scheme);

                try
                {
                    await _notificationSchemeRepository.SaveChangesAsync();
                    schemeIds.Add(scheme.Id);
                }
                catch (Exception ex)
                {
                }
            }

            if (schemeIds.Any())
                await _notificationDistributer.DistributeAsync(schemeIds);
        }
    }

    public interface INotificationDistributer
    {
        Task DistributeAsync(List<long> notificationIds);
    }
    public class NotificationDistributer : INotificationDistributer
    {
        public const string ReceiveNotifications = "NotificationSettings.ReceiveNotifications";
        private const string ReceiveNotificationsDefaultValue = "true";

        private readonly ILogger<NotificationDistributer> Logger;
        private readonly OtpDbContext _context;
        private readonly IRepository<NotificationScheme> _notificationSchemeRepository;
        private readonly IRepository<NotificationDetail> _notificationDetailRepository;
        private readonly IRepositoryWithTypedId<UserNotification, Guid> _userNotificationRepository;
        private readonly IRepository<User> _users;

        public NotificationDistributer(
            ILogger<NotificationDistributer> logger,
            OtpDbContext context,
            IRepository<NotificationScheme> notificationSchemeRepository,
            IRepository<NotificationDetail> notificationDetailRepository,
            IRepositoryWithTypedId<UserNotification, Guid> userNotificationRepository,
            IRepository<User> users)
        {
            Logger = logger;
            _context = context;
            _notificationSchemeRepository = notificationSchemeRepository;
            _notificationDetailRepository = notificationDetailRepository;
            _userNotificationRepository = userNotificationRepository;
            _users = users;
        }

        public async Task DistributeAsync(List<long> notificationIds)
        {
            foreach (var notificationId in notificationIds)
            {
                var notificationScheme = await _notificationSchemeRepository.Query().FirstOrDefaultAsync(s => !s.IsDeleted && s.Id == notificationId);
                if (notificationScheme == null)
                {
                    Logger.LogWarning("NotificationDistributionJob can not continue since could not found notification by id: " + notificationId);
                    return;
                }

                var userIds = await GetUserIds(notificationScheme);

                await SaveUserNotifications(userIds, notificationScheme);

                notificationScheme.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
        }

        private async Task<long[]> GetUserIds(NotificationScheme notificationScheme)
        {
            var userIds = new List<long>();

            foreach (var uid in notificationScheme.UserIds.Split(",").Select(long.Parse))
            {
                if ((await GetSettingValueForUserAsync(uid, ReceiveNotifications)).ToUpper() == "TRUE")
                {
                    userIds.Add(uid);
                }
            }

            if (!string.IsNullOrEmpty(notificationScheme.ExcludedUserIds))
            {
                var excludedUserIds = notificationScheme
                    .ExcludedUserIds
                    .Split(",")
                    .Select(long.Parse)
                    .ToList();

                userIds.RemoveAll(uid => excludedUserIds.Any(euid => euid.Equals(uid)));
            }

            return userIds.ToArray();
        }

        private async Task SaveUserNotifications(long[] userIds, NotificationScheme notificationScheme)
        {
            var notificationDetail = new NotificationDetail(notificationScheme);
            notificationDetail.NotificationSchemeId = notificationScheme.Id;
            notificationDetail.CreatedById = notificationScheme.CreatedById;
            _notificationDetailRepository.Add(notificationDetail);
            await _context.SaveChangesAsync(); 

            foreach (var userId in userIds)
            {
                var userNotification = new UserNotification()
                {
                    UserId = userId,
                    NotificationDetailId = notificationDetail.Id
                };

                _userNotificationRepository.Add(userNotification);
            }
            await _context.SaveChangesAsync();
        }

        private async Task<string> GetSettingValueForUserAsync(long userId, string name)
        {
            var user = await _users.Query().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new ArgumentNullException();
            }

            IDictionary<string, string> settings = null;
            if (user.ExtensionData != null)
            {
                var prop = JObject.Parse(user.ExtensionData)[User.SettingsDataKey];
                if (prop != null)
                {
                    settings = (IDictionary<string, string>)prop.ToObject(typeof(IDictionary<string, string>), JsonSerializer.CreateDefault());
                }
            }
            settings ??= new Dictionary<string, string>();

            var value = (settings.TryGetValue(name, out var custom) ? custom : null) ?? ReceiveNotificationsDefaultValue;
            return value;
        }
    }
}
