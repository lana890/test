namespace EskaCMS.Addresses.Models
{
    public class AddressVM
    {
        public long Id { get; set; }
        public string ContactName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string ZipCode { get; set; }
        public long? DistrictId { get; set; }
        public long? StateOrProvinceId { get; set; }
        public string CountryId { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
    }
}
