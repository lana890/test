using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.UserCatalog.Dtos
{
    public class ProviderTripDetailsVM
    {
        public DateTimeOffset ReservationDate { get; set; }
        public string FromTime { get; set; }
        public string ToTime { get; set; }
        public string ProductName { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerName { get; set; }
        public long UserId { get; set; }
        public long ProviderId { get; set; }
        public decimal Distance { get; set; }
        public string ProviderName { get; set; }
    }
}
