using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Addresses.Model
{
    public class ShippingCostsViewModel
    {
        public long? Id { get; set; }
        public decimal Cost { get; set; }
        public long  StateOrProvinceId { get; set; }
    }

    public class UpdateShippingCostsViewModel
    {
        public long? Id { get; set; }
        public decimal Cost { get; set; }
    }
    public class GetShippingCostsViewModel
    {
        public long Id { get; set; }
        public string Country { get; set; }
        public string CountryId { get; set; }
        public decimal Cost { get; set; }
        public long StateOrProvinceId { get; set; }
        public string StateOrProvince { get; set; }
        public EStatus Status { get; set; }

    }
    public class requestShippingCostsViewModel
    {
        public string SearchText { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }

    }
}
