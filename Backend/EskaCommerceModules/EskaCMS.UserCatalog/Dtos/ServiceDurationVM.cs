using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.UserCatalog.Dtos
{
    public class ServiceDurationVM
    {
        public DateTimeOffset ReservationDate { get; set; }
        public string FromTime { get; set; }
        public string ToTime { get; set; }
        public string ProductName { get; set; }
        public DateTimeOffset StatusDate { get; set; }
        public string Duration { get; set; }

    }
}
