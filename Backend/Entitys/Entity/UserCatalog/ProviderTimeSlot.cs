using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Models;
using System;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.UserCatalog
{
    public class ProviderTimeSlot : EntityBase
    {

        public long ProviderId { get; set; }
        public User Provider { get; set; }
        public TimeSpan FromTime { get; set; }
        public TimeSpan ToTime { get; set; }
        public DayOfWeek Day { get; set; }
        public long CreatedById { get; set; }
        public User CreatedBy { get; set; }
        public long? ModifiedById { get; set; }
        public User ModifiedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }
    }
}
