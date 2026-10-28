using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EskaCMS.Core.Model;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Models.NotificationModels;

namespace EskaCommerce.Notifications.Notifiers
{
    /// <summary>
    /// If we want to use notification service in core module, we can define the 'INotifier' interface and then implement it in other modules, or use event (E.g: <see cref = "UserSignedInHandler" />).
    /// </summary>
    public interface ITestNotifier
    {
        Task WelcomeToTheApplicationAsync(long userId);

        Task NewUserRegisteredAsync(User user);

        Task<SendNotificationDto> SendMessageAsync(NotificationType notificationType,EmailsSettings emailsSettings=null,NotificationObject data=null , long siteId = 0, long? UserId = null);
         
        Task<NotificationObject> ReplaceTemplateParams(NotificationObject data, string emailTemplateId);
        Task SendMeetingEmail(string OrganizerEmail, string OrganizerName, List<string> Atendees, string subject, string body, string location, DateTimeOffset Start, DateTimeOffset End, long SiteId, bool isHtml = true, List<FileViewModel> attachment = null);


    }
}
