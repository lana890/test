using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.UserCatalog.Dtos
{
   public class UserBookingExportVm
    {
        public long BookingId { get; set; }
        public UserBookingStatus Status { get; set; }
        public DateTimeOffset ReservationDate { get; set; }
        public string ProviderName { get; set; }
        public string ServiceName { get; set; }
        public TimeSpan FromTime { get; set; }
        public TimeSpan ToTime { get; set; }

    }
}
