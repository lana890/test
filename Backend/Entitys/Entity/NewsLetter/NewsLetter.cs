
using EskaCMS.Entities.Entity.NewsLetter;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Core
{
    public class NewsLetters:EntityBase
    {
      
        public NewsLetters()
        {
            NewsLetterSubscribers = new HashSet<NewsLetterSubscribers>();
            NewsLetterMedias = new HashSet<NewsLetterMedias>();
        }
        public string Name { get; set; }  


        public string Description { get; set; }

        public EskaCMS.Entities.Enum.ESendStatus? SendStatus { get; set; }
    
        public EskaCMS.Entities.Enum.GeneralEnums.CoreNotificationType NotificationType { get; set; }

        public EskaCMS.Entities.Enum.ESendTimer sendTimer { get; set; }
        public DateTimeOffset? SentDate { get; set; }
        public DateTimeOffset? LastSentDate { get; set; }
        public long SiteId { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.EStatus? Status { get; set; }
        public ICollection<NewsLetterSubscribers> NewsLetterSubscribers { get; set; } 
        public Site Site { get; set; }
        public EskaCMS.Entities.Enum.NewsLetterType Type { get; set; }



        public ICollection<NewsLetterMedias> NewsLetterMedias { get; set; } // *-* with media

        // Start of EmailTemplate FK
        public string? EmailTemplateId { get; set; } = null;
        public long? EmailTemplateSiteId { get; set; } = null; 
        public EmailTemplates EmailTemplate { get; set; }

        // End of EmailTemplate FK
        public string Subject { get; set; }
        public string NewsLetterBody { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public  User CreatedBy { get; set; }
        public  User ModifiedBy { get; set; }
        public long? EmailsSettingsId { get; set; } = 0;

        public EmailsSettings EmailsSettings { get; set; }

        public string NotificationLink { get; set; }

        public bool IsExternalLink { get; set; }

        public DateTimeOffset? EndDate { get; set; }

        public List<NewsLetterGroupSubscribers> GroupSubscribers { get; set; }

        public List<NewsLetterStatistics> NewsLetterStatistics { get; set; }

    }




}
