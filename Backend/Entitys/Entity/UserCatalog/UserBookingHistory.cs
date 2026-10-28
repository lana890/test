using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.UserCatalog
{
    public class UserBookingHistory :EntityBase
    {
        public long UserBookingId { get; set; }
        public UserBooking UserBooking { get; set; }
        public UserBookingStatus? OldStatus { get; set; }
        public UserBookingStatus Status { get; set; }
        public DateTimeOffset StatusDate { get; set; }
     }
}
