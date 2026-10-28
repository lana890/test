using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Models
{
    public class BackgroundServiceSettingsCreate 
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string AdditionalData { get; set; }
        public CoreNotificationType NotificationType { get; set; }
        public IntervalType IntervalType { get; set; }
        public BGPeriodType? PeriodType { get; set; }
        public DateTimeOffset IntervalDate { get; set; }
        public TimeSpan? TimeSpan { get; set; }
        public DayOfWeek? DayOfWeek { get; set; }
        public int? WeekOfMonth { get; set; }
        public int? MonthOfYear { get; set; }
    }
    public class BackgroundServiceSettingsUpdate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string AdditionalData { get; set; }
        public CoreNotificationType? NotificationType { get; set; }
        public IntervalType? IntervalType { get; set; }
        public BGPeriodType? PeriodType { get; set; }
        public DateTimeOffset? IntervalDate { get; set; }
        public TimeSpan? TimeSpan { get; set; }
        public DayOfWeek? DayOfWeek { get; set; }
        public int? WeekOfMonth { get; set; }
        public int? MonthOfYear { get; set; }
    }
    public class BackgroundServiceSettingsList
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public CoreNotificationType NotificationType { get; set; }
        public IntervalType IntervalType { get; set; }
        public BGPeriodType? PeriodType { get; set; }
        public DateTimeOffset IntervalDate { get; set; }
        public TimeSpan? TimeSpan { get; set; }
        public DayOfWeek? DayOfWeek { get; set; }
        public int? WeekOfMonth { get; set; }
        public int? MonthOfYear { get; set; }
    }
    public class BackgroundServiceSettingsListRequest
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public CoreNotificationType? NotificationType { get; set; }
        public DateTimeOffset? IntervalDate { get; set; }
        public TimeSpan? TimeSpan { get; set; }
    }
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
