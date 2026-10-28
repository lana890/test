using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Models.NotificationModels;
using System.Threading.Tasks;

namespace EskaCommerce.Notifications.Services.NotificationService.Interface
{
    public interface IPushNotification
    {
        Task<SendNotificationDto> SendPushNotification(NotificationObject data , long siteId);

    }
}
