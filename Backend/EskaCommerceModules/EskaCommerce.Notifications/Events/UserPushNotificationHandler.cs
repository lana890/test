using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EskaCMS.Core.Events;
using EskaCommerce.Notifications.Notifiers;
using EskaCommerce.Notifications.Services.NotificationService.Interface;

namespace EskaCommerce.Notifications.Events
{
    public class UserPushNotificationHandler : INotificationHandler<UserNotification>
    {

        private readonly IPushNotification _PushNotificationService;
        public UserPushNotificationHandler(IPushNotification PushNotificationService)
        {
            _PushNotificationService = PushNotificationService;
        }

        /// <inheritdoc />
        public async Task Handle(UserNotification notification, CancellationToken cancellationToken)
        {


            Areas.Notifications.ViewModels.NotificationObject notificationObject = new Areas.Notifications.ViewModels.NotificationObject();

            notificationObject.MessageBody = notification.Message;
            notificationObject.MessageTitle = notification.Title;
            notificationObject.NotificationType = Areas.Notifications.ViewModels.NotificationType.PushNotification;
            //notificationObject.PushNotificationOptions.Topic = Areas.Notifications.ViewModels.FireBaseTopics.Marketing;
            notificationObject.PushNotificationOptions.DesitinationType = Areas.Notifications.ViewModels.DesitinationType.Personal;

            //await _PushNotificationService.SendPushNotification(notificationObject);
            //return Task.FromResult(0);
        }
    }
}
