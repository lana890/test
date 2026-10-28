
using EskaCMS.Infrastructure.Models;

using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Entities.Entity.Tax
{
    public class TaxRate : EntityBase
    {
        public long TaxClassId { get; set; }

        public TaxClass TaxClass { get; set; }

        [StringLength(450)]
        public string CountryId { get; set; }

        public EskaCMS.Entities.Entity.Addresses.Country Country { get; set; }

        public long? StateOrProvinceId { get; set; }

        public EskaCMS.Entities.Entity.Addresses.StateOrProvince StateOrProvince { get; set; }

        public decimal Rate { get; set; }

        [StringLength(450)]
        public string ZipCode { get; set; }
    }
}
