using System;

namespace EskaCommerce.UserCatalog.Dtos
{
    public class ProviderTimeSlotCreateVM
    {
        public long ProviderId { get; set; }
        public TimeSpan FromTime { get; set; }
        public TimeSpan ToTime { get; set; }
        public DayOfWeek Day { get; set; }
    }
    public class ProviderTimeSlotOutputVM
    {
        public long Id { get; set; }
        public long ProviderId { get; set; }
        public string ProviderName { get; set; }
        public TimeSpan FromTime { get; set; }
        public TimeSpan ToTime { get; set; }
        public DayOfWeek Day { get; set; }
        public long CreatedById { get; set; }
        public string CreatedBy { get; set; }
        public long? ModifiedById { get; set; }
        public string ModifiedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }
    }
    public class ProviderTimeSlotUpdateVM
    {
        public long Id { get; set; }
        public DayOfWeek? Day { get; set; }
        public TimeSpan? FromTime { get; set; }
        public TimeSpan? ToTime { get; set; }
    }
    public class ProviderTimeSlotVM
    {
        public long Id { get; set; }
        public long ProviderId { get; set; }
        public string ProviderName { get; set; }
        public TimeSpan FromTime { get; set; }
        public TimeSpan ToTime { get; set; }
        public string StringFromTime { get; set; }
        public string StringToTime { get; set; }
        public DayOfWeek Day { get; set; }
        public long CreatedById { get; set; }
        public string CreatedBy { get; set; }
        public long? ModifiedById { get; set; }
        public string ModifiedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }
    }
}
