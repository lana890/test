using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.NotificationManager.Models
{
    public class RequestNewsLetterVm
    {
        public long? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset? SentDate { get; set; }
        public EStatus? Status { get; set; }       
        public Entities.Enum.NewsLetterType Type { get; set; }
        public string? EmailTemplateId { get; set; } = null;
        public IEnumerable<long>? NewsLetterSubscribers { get; set; }

        public EskaCMS.Entities.Enum.GeneralEnums.CoreNotificationType NotificationType { get; set; }

        public long? SenderEmailId { get; set; }
        public Entities.Enum.ESendTimer sendTimer { get; set; }
        public string? Subject { get; set; }
        public string? NewsLetterBody { get; set; }
        public IList<long>? UploadIds { get; set; }

        public bool? IsSend { get; set; }
        public NotificationLinkVM? notificationLinkModel { get; set; }

        public List<long>? NewsLetterGroupSubscribers { get; set; }

        public DateTimeOffset? EndDate { get; set; }

    }

    public class NotificationLinkVM
    {
        public string? NotificationLink { get; set; }
        public string? ExternalLink { get; set; }

        public long? CultureId { get; set; }




    }
}
