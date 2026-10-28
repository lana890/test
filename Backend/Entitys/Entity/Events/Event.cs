
using EskaCMS.Infrastructure.Models;
using System;
using EskaCMS.Entities.Entity.Core;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Events
{
    public class Event : EntityBase
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTimeOffset FromDate { get; set; }
        public DateTimeOffset ToDate { get; set; }
        public DateTimeOffset FromTime { get; set; }
        public DateTimeOffset ToTime { get; set; }
        public TimeSpan Duration { get; set; }
        public EskaCMS.Entities.Enum.EventEnums.Importance Importance { get; set; }
        public EskaCMS.Entities.Enum.EventEnums.RepeatType RepeatType { get; set; }
        public EStatus EventStatus { get; set; }
        public virtual Media Thumbnail { get; set; }

        public long CategoryId { get; set; }
        public virtual Category Category { get; set; }
        public long ThumbnailId { get; set; }
        public long? LocationId { get; set; }

        public EskaCMS.Entities.Enum.EventEnums.PeriodType PeriodType { get; set; }
        public int PeriodInterval { get; set; }
        public virtual EventLocation Location { get; set; }
        public bool SendReminder { get; set; }
        public long MaxEnrollment { get; set; }

        public string Keywords { get; set; }
        public string MetaDescription { get; set; }
        public string AddtionalMetaTags { get; set; }
        public string Settings { get; set; }
        public long SiteId { get; set; }
        public long CultureId { get; set; }
        public virtual SitesCultures Culture { get; set; }
        public long CreatedById { get; set; }
        public virtual User CreatedBy { get; set; }

        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public virtual User ModifiedBy { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public long? ModifiedById { get; set; }

        public long NumberOfDays { get; set; }

        public EskaCMS.Entities.Enum.EventEnums.EventPlace EventPlace { get; set; }

        public long? ParentId { get; set; }

        public Event Parent { get; set; }
    }


}
