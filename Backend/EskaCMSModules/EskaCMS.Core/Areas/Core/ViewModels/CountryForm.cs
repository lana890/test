using EskaCMS.Infrastructure.Web.SmartTable;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class CountryForm
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        public string Id { get; set; }

        public string Name { get; set; } 
        public string Code3 { get; set; }

        public bool IsBillingEnabled { get; set; }

        public bool IsShippingEnabled { get; set; }

        public bool IsCityEnabled { get; set; }

        public bool IsZipCodeEnabled { get; set; }

        public bool IsDistrictEnabled { get; set; }
        public bool IsWebCountryView { get; set; }
        public bool IsRegional { get; set; }

    }
    public class CountryRequestForm
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        public string Id { get; set; }

        public IList<CountryDetailsForm> countryDetails { get; set; }
        public string Code3 { get; set; }

        public bool IsBillingEnabled { get; set; }

        public bool IsShippingEnabled { get; set; }

        public bool IsCityEnabled { get; set; }

        public bool IsZipCodeEnabled { get; set; }

        public bool IsDistrictEnabled { get; set; }
        public bool IsWebCountryView { get; set; }
        public bool IsRegional { get; set; }

    }
    public class CountryDetailsForm
     {
        public string Name { get; set; }
        public long CultureId { get; set; }


    }
    public class CountrySearchListVM
    {
        public string Name { get; set; }
 
    }

    public class CountrySmartSearchTableParam<T>
    {
        public Pagination Pagination { get; set; }

        public T Search { get; set; }

    }
    public class CountrySmartGridOutputVM<T>
    {
        public List<T> Items { get; set; }
        public int TotalRecord { get; set; }
    }

    public class CountryReponseDto
    {

        public string Id { get; set; }
        public string Name { get; set; }
    }
}
