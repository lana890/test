using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EskaCMS.Entities.Entity.ShippingTableRate;
using EskaCMS.Entities.Entity.Vendors;
using EskaCMS.Infrastructure.Models;

namespace EskaCMS.Entities.Entity.Inventory

{
    public class Warehouse : EntityBase
    {
        public Warehouse() { }

        public Warehouse(long id)
        {
            Id = id;
        }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }

        public long AddressId { get; set; }

        public long? VendorId { get; set; }

        public Vendor Vendor { get; set; }

        public EskaCMS.Entities.Entity.Addresses.Address Address { get; set; }

        public long SiteId { get; set; }
        public EskaCMS.Entities.Entity.Core.Site Site { get; set; }


        public bool IsMain { get; set; }
        public bool IsVisible { get; set; }

        public IList<PriceAndDestination>? priceAndDestinations { get; set; }
        public List<WarehouseRoles> WarehouseRoles { get; set; }
        public List<WarehouseWorkingHours>  WarehouseWorkingHours { get; set; }

    }
}
