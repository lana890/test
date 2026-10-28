using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
    public class OrderAddressViewModel
    {
        public long? Id { get; set; }
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

    public class OrderAddresssResponseViewModel
    {
        public long Id { get; set; }
        public string ContactName { get; set; }

        public string Phone { get; set; }

        public string Email { get; set; }

        public string AddressLine1 { get; set; }

        public string AddressLine2 { get; set; }

        public string City { get; set; }

        public string ZipCode { get; set; }

        public string District { get; set; }


        public string StateOrProvince { get; set; }
        public string CountryId { get; set; }

        public long? StateOrProvinceId { get; set; }
        public long? DistrictId { get; set; }


        public string Country { get; set; }
        public long AddressId { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string Regionphonenumber { get; set; }
    }
}
