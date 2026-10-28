using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Model;
using EskaCMS.Core.Services;
using EskaCMS.EmailSender.SMTP;
using EskaCMS.EmailSender.SMTP.Services;
using EskaCommerce.Notifications;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Notifiers;
using EskaCommerce.Notifications.Services;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MimeKit.Text;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using System.Text.RegularExpressions;
using EskaCMS.Infrastructure.Extensions;
using Newtonsoft.Json;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Notifications.Hubs;
using Microsoft.AspNetCore.SignalR;
using EskaCMS.Core.Services.Interfaces;
using EskaCommerce.Notifications.Repository.NotificationRepository.Interface;
using EskaCommerce.Notifications.Models.NotificationModels;
using EskaCommerce.Notifications.Services.NotificationService.Interface;

namespace EskaCommerce.Module.Notifications.Notifiers
{
    public class TestNotifier : ITestNotifier
    {
        private readonly INotificationPublisher _notificationPublisher;
        private readonly IPushNotification _pushNotification;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;
        private readonly UserManager<User> _userManager;
        private readonly IWorkContext _workContext;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly NotificationTracker notificationTracker;
        private readonly IHubContext<NotificationHub> hubNotificationhub;
        private readonly INotificationRepository notificationRepository;
        private readonly IWhatsAppServices whatsAppServices;
        private readonly IdcmsLogService _dcmsLogService;
        private readonly ISiteSettingsService _settingservice;

        public TestNotifier(INotificationPublisher notificationPublisher, IPushNotification pushNotification,
            IEmailSender emailSender,
           ISmsSender smsSender,
            UserManager<User> userManager,
            IEmailTemplateService emailTemplateService,
            NotificationTracker notificationTracker,
            IHubContext<NotificationHub> hubNotificationhub,
                        INotificationRepository notificationRepository,
                        IWhatsAppServices whatsAppServices,
            IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository,
            ISiteSettingsService settingservice,
            IWorkContext workContext, IdcmsLogService dcmsLogService)
        {
            _notificationPublisher = notificationPublisher;
            _pushNotification = pushNotification;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _userManager = userManager;
            _workContext = workContext;
            _emailTemplateService = emailTemplateService;
            this.notificationTracker = notificationTracker;
            this.hubNotificationhub = hubNotificationhub;
            this.notificationRepository = notificationRepository;
            this.whatsAppServices = whatsAppServices;
            _dcmsLogService = dcmsLogService;
            _settingservice = settingservice;
        }

        public async Task WelcomeToTheApplicationAsync(long userId)
        {
            // Publish to new user
            var source = _workContext.GetSource();
            var SiteId = _workContext.GetCurrentSiteId();
            if (source !=ETypeLogin.Mobile)
            {
                await _notificationPublisher.PublishAsync(
                    NotificationType.WebNotification.ToString(),
                    new MessageNotificationData("Welcome to store."),
                    severity: NotificationSeverity.Success,
                    userIds: new[] { userId }, EmailTemplateId: null
                    );
            }
            else
            {
                var lstSendValue = new List<string>();
                lstSendValue.Add(userId.ToString());
                var lstuser = new long[1];
                lstuser[0] = userId;
                NotificationObject objdata = new NotificationObject();
                objdata.MessageTitle = "Welcome";
                objdata.MessageBody = "Welcome to app";
                objdata.NotificationType = NotificationType.PushNotification;
                objdata.SendValue = lstSendValue;
                objdata.SendVia = SendBy.UserId;
                objdata.UserId = lstuser;
                objdata.Severity = NotificationSeverity.Success;
                objdata.PushNotificationOptions.DesitinationType = DesitinationType.Personal;
                await SendMessageAsync(NotificationType.PushNotification, null, objdata, SiteId);

            }
        }

        public async Task NewUserRegisteredAsync(User user)
        {
            var notificationData = new MessageNotificationData("New user registered.");

            notificationData["userName"] = user.UserName;
            notificationData["email"] = user.Email;

            // Publish to all subscribed users
            await _notificationPublisher.PublishAsync(NotificationDefinitions.Names.NewUserRegistered, notificationData, EmailTemplateId: null);
        }

        public async Task<SendNotificationDto> SendMessageAsync(
     NotificationType notificationType,
     EmailsSettings EmailSettings = null,
     NotificationObject data = null,
     long siteId = 0,
     long? UserId = null)
        {
            try
            {
                List<long> lstOfUserIds = new List<long>();
                var notificationRsponse = new SendNotificationDto();

                if (notificationType == NotificationType.PushNotification)
                {
                    var pushNotificationRes = await _pushNotification.SendPushNotification(data, siteId);
                    data.UserId = pushNotificationRes.UserIds;
                    notificationRsponse.FailureCount = pushNotificationRes.FailureCount;
                    notificationRsponse.SuccessCount = pushNotificationRes.SuccessCount;
                }
                else if (notificationType == NotificationType.SMS)
                {
                    if (data.UserInfoWithParams != null)
                    {
                      
                        foreach (var user in data.UserInfoWithParams)
                        {
                            try
                            {
                                var smsProvider = data.SMSProvider ?? SMSProviders.SMSMashastra;
                                await _dcmsLogService.AddInfo("Notification Module",
                                    $"Sending SMS To : {JsonConvert.SerializeObject(data.SendValue)}",
                                    JsonConvert.SerializeObject(data), siteId);

                                data = await ReplaceTemplateParamsPerUser(data, user.Value, data.EmailTemplateId);
                                data.MessageBody = HtmlToPlainText(data.MessageBody);

                                await _smsSender.SendSmsAsync(smsProvider, user.Key, data.MessageBody, siteId);
                                await _dcmsLogService.AddInfo("Notification Module",
                                    $"Send SMS Successfully To : {JsonConvert.SerializeObject(user.Key)}",
                                    JsonConvert.SerializeObject(data), siteId);

                                // ✅ FIX: wrapped in try/catch — non-critical, must not kill the loop
                                try
                                {
                                    User userInDb = await _userManager.Users
                                        .AsNoTracking()
                                        .Where(x => x.PhoneNumber.Contains(user.Key))
                                        .FirstOrDefaultAsync();

                                    if (userInDb != null)
                                        lstOfUserIds.Add(userInDb.Id);
                                    else
                                    {
                                        var currentUser = await _workContext.GetCurrentUser();
                                        if (currentUser != null)
                                            lstOfUserIds.Add(currentUser.Id);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    await _dcmsLogService.AddInfo("Notification",
                                        "Could not resolve user for SMS notification", ex.Message, siteId);
                                }
                            }
                            catch (Exception e)
                            {
                                await _dcmsLogService.AddError("SMS Sender (Unifonic)", e.Message,
                                    "Cannot Send SMS for : " + user.Key, data.MessageBody, siteId);
                            }
                        }
                    }
                    else if (data.EmailOptions != null && data.EmailOptions.Params != null)
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

                    if (data.SendValue.Count > 0 && data.EmailOptions.Emails.Count > 0)
                    {
                        if (data.EmailOptions?.Params != null)
                        {
                            await _dcmsLogService.AddInfo("Notification Module",
                                $"Sending Email To : {JsonConvert.SerializeObject(data.SendValue)}",
                                JsonConvert.SerializeObject(data), siteId);

                            data = await ReplaceTemplateParams(data, data.EmailOptions.TemplateId);
                            await _emailSender.SendEmailAsync(data.EmailOptions.Emails,
                                data.EmailOptions.SenderModule, data.MessageTitle, data.MessageBody,
                                siteId, EmailSettings, isHTML, data.Attachment, data.CC, data.Files);

                            if (!string.IsNullOrEmpty(data.EmailTemplateId))
                            {
                                data = await ReplaceTemplateParams(data, data.EmailTemplateId);
                                await _emailSender.SendEmailAsync(data.SendValue, null,
                                    data.MessageTitle, data.MessageBody, siteId, EmailSettings,
                                    isHTML, data.Attachment, data.CC,data.Files);
                            }

                            await _dcmsLogService.AddInfo("Notification Module",
                                $"Successfully sent Email To : {JsonConvert.SerializeObject(data.SendValue)}",
                                JsonConvert.SerializeObject(data), siteId);
                        }
                    }
                    else if (data.UserInfoWithParams != null)
                    {
                        // ✅ FIX: was Task.Run(...).ConfigureAwait(false) — same fire-and-forget bug.
                        // Awaited directly now.
                        foreach (var user in data.UserInfoWithParams)
                        {
                            try
                            {
                                await _dcmsLogService.AddInfo("Notification Module",
                                    $"Sending Email To : {JsonConvert.SerializeObject(user.Key)}",
                                    JsonConvert.SerializeObject(data), siteId);

                                data = await ReplaceTemplateParamsPerUser(data, user.Value, data.EmailTemplateId);
                                var sendValue = new List<string> { user.Key };
                                await _emailSender.SendEmailAsync(sendValue, data.EmailOptions.SenderModule,
                                    data.MessageTitle, data.MessageBody, siteId, EmailSettings, isHTML, data.Attachment);

                                await _dcmsLogService.AddInfo("Notification Module",
                                    $"Successfully send Email To : {JsonConvert.SerializeObject(data.SendValue)}",
                                    JsonConvert.SerializeObject(data), siteId);
                            }
                            catch (Exception e)
                            {
                                await _dcmsLogService.AddError("Notification Module", e.Message,
                                    $"Error Send Notification To : {JsonConvert.SerializeObject(user.Key)}",
                                    JsonConvert.SerializeObject(data), siteId);
                            }
                        }
                    }
                    else
                    {
                        if (data.EmailOptions?.Params != null)
                            data = await ReplaceTemplateParams(data, data.EmailOptions.TemplateId);

                        await _dcmsLogService.AddInfo("Notification Module",
                            $"Sending Email To : {JsonConvert.SerializeObject(data.SendValue)}",
                            JsonConvert.SerializeObject(data), siteId);

                        await _emailSender.SendEmailAsync(data.SendValue, data.EmailOptions.SenderModule,
                            data.MessageTitle, data.MessageBody, siteId, EmailSettings, isHTML,
                            data.Attachment, data.CC, data.Files);

                        await _dcmsLogService.AddInfo("Notification Module",
                            $"Successfully send Email To : {JsonConvert.SerializeObject(data.SendValue)}",
                            JsonConvert.SerializeObject(data), siteId);
                    }

                    // ✅ FIX: user lookup wrapped in try/catch — DbContext may not be available
                    // in all calling contexts (e.g. called from a background job scope).
                    // This is non-critical — lstOfUserIds is only used for the notification publisher.
                    foreach (string item in data.SendValue)
                    {
                        if (string.IsNullOrEmpty(item)) continue;
                        try
                        {
                            var normalized = item.ToUpper();
                            User user = await _userManager.Users
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
                                "Could not resolve user for email notification", ex.ToJsonString(), siteId);
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
                else if (notificationType == NotificationType.SignalR)
                {
                    foreach (string item in data.SendValue)
                    {
                        if (string.IsNullOrEmpty(item)) continue;
                        try
                        {
                            User user = await _userManager.Users
                                .AsNoTracking()
                                .Where(x => x.NormalizedEmail.Contains(item.ToUpper()))
                                .FirstOrDefaultAsync();

                            if (user != null)
                                lstOfUserIds.Add(UserId ?? user.Id);
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
                                "Current User Null", ex.ToJsonString(), siteId);
                        }
                    }

                    data.UserId = lstOfUserIds.ToArray();

                    if (data.EmailOptions?.Params != null)
                    {
                        data = await ReplaceTemplateParams(data, data.EmailOptions.TemplateId);
                        if (!string.IsNullOrEmpty(data.EmailTemplateId))
                            data = await ReplaceTemplateParams(data, data.EmailTemplateId);
                    }
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

                    if (notificationType == NotificationType.SignalR)
                        await SendRealTimeNotification(data.UserId);
                }

                return notificationRsponse;
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

                throw;  // ✅ FIX: was "throw ex" which resets the stack trace — use "throw" instead
            }
        }


        public async Task SendMeetingEmail(string OrganizerEmail, string OrganizerName, List<string> Atendees, string subject, string body, string location, DateTimeOffset Start, DateTimeOffset End, long SiteId, bool isHtml = true, List<FileViewModel> attachment = null)
        {
            try
            {
                await _emailSender.SendMeetingEmailAsync(OrganizerEmail, OrganizerName, Atendees, subject, body, location, Start, End, SiteId, isHtml, attachment);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task SendRealTimeNotification(long[] UserIds)
        {
            try
            {
                if (UserIds.Count() > 0)
                {
                    foreach (long userId in UserIds)
                    {
                        var isOnline = await NotificationTracker.GetConnectionsForUser(userId);
                        if (isOnline != null && isOnline.Count() > 0)
                        {



                            var signalRClient = hubNotificationhub.Clients.Clients(isOnline);
                            var Notification = await notificationRepository.GetLastNotifications(1, 0, userId, NotificationType.SignalR);
                            if (signalRClient == null)
                            {
                                continue;
                            }
                            await signalRClient.SendAsync("getLastNotification", Notification);

                        }

                    }
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        // Compile these constant-pattern regexes once instead of per HtmlToPlainText call.
        private static readonly Regex _lineBreakRegex = new Regex(@"<(br|BR)\s{0,1}\/{0,1}>", RegexOptions.Multiline);
        private static readonly Regex _stripFormattingRegex = new Regex(@"<[^>]*(>|$)", RegexOptions.Multiline);
        private static readonly Regex _tagWhiteSpaceRegex = new Regex(@"(>|$)(\W|\n|\r)+<", RegexOptions.Multiline);

        private string HtmlToPlainText(string html)
        {
            var lineBreakRegex = _lineBreakRegex;
            var stripFormattingRegex = _stripFormattingRegex;
            var tagWhiteSpaceRegex = _tagWhiteSpaceRegex;

            var text = html;
            //Decode html specific characters
            text = System.Net.WebUtility.HtmlDecode(text);
            //Remove tag whitespace/line breaks
            text = tagWhiteSpaceRegex.Replace(text, "><");
            //Replace <br /> with line breaks
            text = lineBreakRegex.Replace(text, Environment.NewLine);
            //Strip formatting
            text = stripFormattingRegex.Replace(text, string.Empty);
            text = System.Net.WebUtility.HtmlDecode(text);


            return text;
        }

        public async Task<NotificationObject> ReplaceTemplateParamsPerUser(NotificationObject data, List<EmailParamsVM> Params, string emailTemplateId)
        {

            var emailTemplate = await _emailTemplateService.GetEmailTemplateById(emailTemplateId);

            if (emailTemplate != null)
            {


                if (Params.Count() > 0)
                {
                    foreach (var item in Params)
                    {
                        emailTemplate.EmailBody = emailTemplate.EmailBody.Replace(item.Param, item.Value);
                    }

                    data.MessageTitle = string.IsNullOrEmpty(data.MessageTitle) ? emailTemplate.Subject : data.MessageTitle;
                    data.MessageBody = emailTemplate.EmailBody;
                    data.isHTML = true;
                }
                else
                {
                    data.isHTML = emailTemplate.IsHTML;
                }
            }

            return data;

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
                    data.MessageTitle =string.IsNullOrEmpty(data.MessageTitle) ? emailTemplate.Subject: data.MessageTitle;
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

                // To Be Removed
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
                var user = await _userManager.Users
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


                User user = await _userManager.Users.Where(x => x.UserName.ToLower().Contains(data.UserName)).FirstOrDefaultAsync();

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
                    await whatsAppServices.SendMessageAsync(WhatsAppProvider.Value, value, user?.FullName ?? "Customer",data.IsPdf,data.PdfLink, data.MessageBody, siteId);

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
}
