using System.ComponentModel.DataAnnotations;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Infrastructure.Models;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.ShippingTableRate
{
    public class PriceAndDestination : EntityBase
    {
        public EskaCMS.Entities.Entity.Addresses.Country Country { get; set; }

        [StringLength(450)]
        public string CountryId { get; set; }

        public EskaCMS.Entities.Entity.Addresses.StateOrProvince StateOrProvince { get; set; }

        public long? StateOrProvinceId { get; set; }

        public EskaCMS.Entities.Entity.Addresses.District District { get; set; }

        public long? DistrictId { get; set; }

        [StringLength(450)]
        public string ZipCode { get; set; }

        public string Note { get; set; }

        public long SiteId { get; set; }
        public Site Site { get; set; }

        public decimal MinOrderSubtotal { get; set; }

        public decimal ShippingPrice { get; set; }
        public int? NoOfItems { get; set; }
        public long WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; }

        public EStatus Status { get; set; }
        public int? EstimatedDeliveryDate { get; set; }



    }
}
