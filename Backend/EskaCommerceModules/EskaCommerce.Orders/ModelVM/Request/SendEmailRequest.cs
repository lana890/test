using EskaCMS.Entities.Entity.Core;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Orders.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Orders.ModelVM.Request
{
    public class SendEmailRequest
    {
        public NotificationType notificationType { get; set; }
        public EmailsSettings EmailSettings { get; set; }
        public NotificationObject data { get; set; }
        public long siteId { get; set; }
        public long? UserId { get; set; }
        public EmailType EmailType { get; set; }
    public string EmailId {  get; set; }
       
    }
}
