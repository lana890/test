namespace EskaCommerce.UserCatalog.Dtos
{
    public class LocationStremingVM
    {
        public long? UserBookingId { get; set; }
        public long? UserId { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string ConnectionId { get; set; }
    }
}
