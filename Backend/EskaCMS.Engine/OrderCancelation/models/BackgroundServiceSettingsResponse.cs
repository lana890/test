using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Engine.OrderCancelation.models
{
    public class BackgroundServiceSettingsResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string AdditionalData { get; set; }
        public long CreatedById { get; set; }
        public string CreatedBy { get; set; }
        public CoreNotificationType? NotificationType { get; set; }
        public IntervalType? IntervalType { get; set; }
        public BGPeriodType? PeriodType { get; set; }
        public DateTimeOffset? IntervalDate { get; set; }
        public TimeSpan? TimeSpan { get; set; }
        public DayOfWeek? DayOfWeek { get; set; }
        public int? WeekOfMonth { get; set; }
        public int? MonthOfYear { get; set; }
        public bool IsActive { get; set; }
        public long SiteId { get; set; }
    }
}
