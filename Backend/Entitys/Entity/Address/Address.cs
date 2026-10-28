using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Address;
using System.ComponentModel.DataAnnotations;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Addresses
{
    public class Address : Infrastructure.Models.EntityBase
    {
        //public Address() { }

        //public Address(long id)
        //{
        //    Id = id;
        //}

        [StringLength(450)]
        public string ContactName { get; set; }

        [StringLength(450)]
        public string Phone { get; set; }

        [StringLength(450)]
        public string Email { get; set; }

        [StringLength(450)]
        public string AddressLine1 { get; set; }

        [StringLength(450)]
        public string AddressLine2 { get; set; }

        [StringLength(450)]
        public string City { get; set; }

        [StringLength(450)]
        public string ZipCode { get; set; }

        public long? DistrictId { get; set; }

        public District District { get; set; }


        //[Required(ErrorMessage = "The {0} field is required.")]
        public long? StateOrProvinceId { get; set; }

        public StateOrProvince StateOrProvince { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string CountryId { get; set; }

        public Country Country { get; set; }

        public string Latitude { get; set; }
        public string Longitude { get; set; }

        public long SiteId { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }

        public EStatus Status { get; set; }

    }
}
