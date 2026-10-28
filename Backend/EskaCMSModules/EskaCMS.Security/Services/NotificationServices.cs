using EskaCMS.Security.Models.FriendshipModels;
using EskaCMS.Core.Models;
using EskaCMS.EmailSender.SMTP.Services;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Notifiers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Security.Services.Interface;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EskaCMS.Security.Services
{
    public class NotificationServices : INotificationServices
    {
        private readonly IEmailTemplateService emailTemplateService;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly IRepository<EmailsSettings> emailsSettingsRepository;
        private readonly ITestNotifier testNotifier;

        public NotificationServices(IEmailTemplateService emailTemplateService, 
            IServiceScopeFactory serviceScopeFactory,
            IRepository<EmailsSettings> emailsSettingsRepository,
            ITestNotifier testNotifier

)
        {
            this.emailTemplateService = emailTemplateService;
            this.serviceScopeFactory = serviceScopeFactory;
            this.emailsSettingsRepository = emailsSettingsRepository;
            this.testNotifier = testNotifier;
        }

        public async Task<bool> SendMessage(NotificationDto notification)
        {
            try
            {
                using (var scope = serviceScopeFactory.CreateScope())
                {
                    NotificationObject Notificationobj = await CreateNotificationObject(notification , scope);

                    var testNotifier = scope.ServiceProvider.GetService<ITestNotifier>();

                    await testNotifier.SendMessageAsync(NotificationType.SignalR, null, Notificationobj, notification.SiteId, notification.RecipientUserId);
                   
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private async Task SendNotificationAsync(NotificationDto notification, NotificationObject notificationObject)
        {
            try
            {
                var  EmailSetting = await emailsSettingsRepository.Query().Where(x => x.IsDefault).FirstOrDefaultAsync();

                var notificationResponse = await testNotifier.SendMessageAsync(NotificationType.SignalR, EmailSetting, notificationObject, notification.SiteId,notification.RecipientUserId);


            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<NotificationObject> CreateNotificationObject(NotificationDto notification , IServiceScope scope)
        {
            NotificationObject notificationObj = new NotificationObject();

            var emailTemplateService = scope.ServiceProvider.GetService<IEmailTemplateService>();

            var objCustomerEmailTemplate = await emailTemplateService.GetEmailTemplateById(notification.EmailTemplateId.ToString());

            await SetCommonProperties(notificationObj, objCustomerEmailTemplate, notification);

           

            return notificationObj;
        }
     

        private async Task SetCommonProperties(NotificationObject notificationObj, EmailTemplateVM emailTemplate, NotificationDto notification)
        {
            notificationObj.MessageTitle = emailTemplate.Subject;
            notificationObj.MessageBody = emailTemplate.EmailBody;
            notificationObj.SendVia = SendBy.Email;

            notificationObj.Severity = 0;
            notificationObj.NotificationType = NotificationType.SignalR;
            notificationObj.EmailOptions.TemplateId = emailTemplate?.Id;
            notificationObj.EmailOptions.Params = new List<EmailSender.SMTP.Areas.ViewModels.EmailParamsVM> 
                        { new EmailSender.SMTP.Areas.ViewModels.EmailParamsVM { Param ="Content" ,Value= notification.FullName} ,
                          new EmailSender.SMTP.Areas.ViewModels.EmailParamsVM  { Param = "[sender-user]", Value= notification.SenderFullName}, 
                          new EmailSender.SMTP.Areas.ViewModels.EmailParamsVM   { Param = "[receiver-user]", Value= notification.SenderFullName} };
            notificationObj.EmailOptions.Emails = new List<string> { notification.Email }; 
            notificationObj.SendValue = new List<string> { notification.Email }; 
        }

    }
}
