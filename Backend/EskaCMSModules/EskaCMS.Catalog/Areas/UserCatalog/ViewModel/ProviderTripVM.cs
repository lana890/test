using System;

namespace EskaCommerce.UserCatalog.Dtos
{
    public class ProviderTripCreateVM
    {

        public long ProviderId { get; set; }
        public long BookingId { get; set; }
        public decimal Distance { get; set; }
        public TimeSpan DistanceTime { get; set; }
    }

}
