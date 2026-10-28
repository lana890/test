using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Infrastructure.Models;
using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.UserCatalog
{
    public  class ProviderTrip :EntityBase
    {
        public long ProviderId { get; set; }
        public User Provider { get; set; }
        public long BookingId { get; set; }
        public UserBooking Booking { get; set; }
        public decimal Distance { get; set; }
    }
}
