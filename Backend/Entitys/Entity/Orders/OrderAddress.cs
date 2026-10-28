using EskaCMS.Entities.Entity.Address;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Orders
{
    public class OrderAddress : EntityBase
    {
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

        public EskaCMS.Entities.Entity.Addresses.District District { get; set; }

        public long? StateOrProvinceId { get; set; }

        public EskaCMS.Entities.Entity.Addresses.StateOrProvince StateOrProvince { get; set; }

        [StringLength(450)]
        public string CountryId { get; set; }

        public EskaCMS.Entities.Entity.Addresses.Country Country { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }

    }



}
