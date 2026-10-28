namespace EskaCMS.Entities.Entity
{
    public class TaxRateVM
    {
        public string CountryId { get; set; }
        public long? StateOrProvinceId { get; set; }
        public decimal Rate { get; set; }
        public string ZipCode { get; set; }
    }
}
