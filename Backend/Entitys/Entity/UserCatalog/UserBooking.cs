
using EskaCMS.Infrastructure.Models;
using EskaCMS.Entities.Entity.Catalog;

using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.UserCatalog
{
    public class UserBooking : EntityBase
    {

        public User User { get; set; }
        public long UserId { get; set; }

        public long ProviderId { get; set; }

        public User Provider { get; set; }

        public EskaCMS.Entities.Entity.Addresses.UserAddress UserAddress { get; set; }
        public long UserAddressId { get; set; }

        public DateTimeOffset ReservationDate { get; set; }
        public TimeSpan ReservationTime { get; set; }

        public UserPlans UserPlan { get; set; }
        public long UserPlanId { get; set; }
        public Product Product { get; set; }
        public long ProductId { get; set; }
        public UserBookingStatus Status { get; set; } 
        public DateTimeOffset StatusDate { get; set; }
        public User CreatedBy { get; set; }
        public long CreatedById { get; set; }
        public string ConnectionId { get; set; }

        public DateTimeOffset CreationDate { get; set; }
        public long? ProviderTimeSlotId { get; set; }
        public ProviderTimeSlot ProviderTimeSlot { get; set; }

    }
}
