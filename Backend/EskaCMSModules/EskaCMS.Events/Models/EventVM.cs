using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using static EskaCMS.Entities.Enum.EventEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Events.Models
{
    public class EventVM
    {
        public DateTimeOffset FromDate { get; set; }
        public DateTimeOffset ToDate { get; set; }
        public DateTimeOffset RequestDate { get; set; }
        public DateTimeOffset FromTime { get; set; }
        public DateTimeOffset ToTime { get; set; }
        public TimeSpan Duration { get; set; }
        public Importance Importance { get; set; }
        public RepeatType RepeatType { get; set; }
        public EStatus EventStatus { get; set; }
        public EventPlace EventPlace { get; set; }
        public PeriodType PeriodType { get; set; }
        public int PeriodInterval { get; set; }
        public long ThumbnailId { get; set; }
        public string ThumbnailPath { get; set; }
        public long? LocationId { get; set; }
        public long Id { get; set; }
        public long? ParentId { get; set; }
        public long NumberOfDays { get; set; }

        public long  CategoryId { get; set; }
        public bool SendReminder { get; set; }
        public long MaxEnrollment { get; set; }

        public string Keywords { get; set; }
        public string MetaDescription { get; set; }
        public string AdditionalMetaTags { get; set; }
        public string Settings { get; set; }
        public long SiteId { get; set; }
    
        public long CreatedById { get; set; }
        public string CoreUsername { get; set; }

        public DateTimeOffset CreationDate { get; set; }
      
        public DateTimeOffset? ModificationDate { get; set; }
        public long? ModifiedById { get; set; }

        public ContentVM Content { get; set; }
    }

    public class ContentVM
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public long CultureId { get; set; }

    }


}
