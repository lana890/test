using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EskaCMS.Entities.Entity.Address;
using EskaCMS.Infrastructure.Models;
using static EskaCMS.Entities.Enum.GeneralEnums;


namespace EskaCMS.Entities.Entity.Addresses
{
    public class Country : EntityBaseWithTypedId<string>
    {
        public Country(string id)
        {
            Id = id;
        }
        [StringLength(450)]
        public string Code3 { get; set; }

        public bool IsBillingEnabled { get; set; }

        public bool IsShippingEnabled { get; set; }

        public bool IsCityEnabled { get; set; } = true;

        public bool IsZipCodeEnabled { get; set; } = true;

        public bool IsDistrictEnabled { get; set; } = true;
        public bool IsWebCountryView { get; set; }
        public EStatus Status { get; set; }

        public IList<StateOrProvince> StatesOrProvinces { get; set; } = new List<StateOrProvince>();
        public IList<CountryDetails> CountryDetails { get; set; } = new List<CountryDetails>();
        public bool IsRegional { get; set; }

    }
}
