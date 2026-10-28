using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class ProductAvaliablity : EntityBase
    {
        public long ProductId { get; set; }

        public Product Product { get; set; }

        public long? StateOrProvinceId { get; set; }

        public StateOrProvince StateOrProvince { get; set; }

        public string CountryId { get; set; }

        public Country Country { get; set; }

        public EProductCountryPermission  Permission { get; set; }
    }
}
