using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace  EskaCMS.Entities.Entity.Core

{
    public class BackgroundServiceSettings : EntityBaseWithTypedId<string>
    {
        // the Id will be name of the service

        // Name & Title & Body & AdditionalData used for Notifications
        public string Name { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string AdditionalData { get; set; }
        public CoreNotificationType NotificationType { get; set; }

        public IntervalType IntervalType { get; set; }// date or period

        // if IntervalType is Date then you will use IntervalDate
        public DateTimeOffset IntervalDate { get; set; }

        // if IntervalType is Period then you will use one of these options:
        // ( TimeSpan MonthOfYear , WeekOfMonth , DayOfWeek) Depends On PeriodType Value
        public  BGPeriodType? PeriodType { get; set; }
        public TimeSpan TimeSpan { get; set; }
        public DayOfWeek? DayOfWeek { get; set; }
        public int? WeekOfMonth { get; set; }
        public int? MonthOfYear { get; set; }


        public long CreatedById { get; set; }
        public virtual User CreatedBy { get; set; }
        public long ModifiedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public virtual User ModifidedBy { get; set; }
        public virtual Site Site { get; set; }
        public long SiteId { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset? LastExecuted { get; set; }
    }
}
