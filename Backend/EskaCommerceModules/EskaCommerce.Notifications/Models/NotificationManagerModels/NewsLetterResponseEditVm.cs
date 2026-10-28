using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.NotificationManager.Models
{
    public class NewsLetterResponseEditVm
    {
        public long? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset? SentDate { get; set; }
        public EStatus? Status { get; set; }
        public Entities.Enum.ESendTimer SendTimer { get; set; }   
 
        public Entities.Enum.NewsLetterType Type { get; set; }
        public string? EmailTemplateId { get; set; }
        public  List<UsersNewsLetter>? Users { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        [EmailAddress]
        public EmailSettingsDto EmailSettingsResponse { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.CoreNotificationType NotificationType { get; set; }

        public string? Subject { get; set; }
        public string? NewsLetterBody { get; set; }
        public List<FileViewModelDto>? AttachFile { get; set; }

        public DateTimeOffset? EndDate { get; set; }

        public string NotificationLink { get; set; }
        public NotificationLinkVM? NotificationLinkVM { get; set; }

        public List<SubscribedGroupDto>? SubscribedGroup { get; set; }
        public bool IsExternalLink { get;  set; }
    }
}
