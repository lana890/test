using EskaCMS.Entities.Entity.Core;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Pricing.Services.Interfaces
{
    public interface ISendRuleSms
    {
        Task SendMessageAsync(NotificationType notificationType, EmailsSettings EmailSettings = null, NotificationObject data = null, long siteId = 0);
    }
}
