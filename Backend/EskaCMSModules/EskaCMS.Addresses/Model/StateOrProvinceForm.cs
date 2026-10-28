using EskaCMS.Infrastructure.Web.SmartTable;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Addresses.ViewModels
{
    public class StateOrProvinceForm
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string CountryId { get; set; }

        public string CountryCode { get; set; }

        public string Code { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Name { get; set; }

        public string Type { get; set; }
    }

    public class StateSearchListVM
    {
        public string Name { get; set; }

    }

    public class StateSmartSearchTableParam<T>
    {
        public Pagination Pagination { get; set; }

        public T Search { get; set; }

    }
    public class StateSmartGridOutputVM<T>
    {
        public List<T> Items { get; set; }
        public int TotalRecord { get; set; }
    }
    public class StateOrProvinceResponseRequestForm
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string CountryId { get; set; }

        public string CountryCode { get; set; }

        public string Code { get; set; }

        public IList<StateOrProvinceDetailsVm> StateOrProvinceDetails { get; set; } = new List<StateOrProvinceDetailsVm>();

        public string Type { get; set; }
    }

    public class StateOrProvinceDetailsVm
    {
        public string Name { get; set; }
        public long cultureId { get; set; }

    }

}
