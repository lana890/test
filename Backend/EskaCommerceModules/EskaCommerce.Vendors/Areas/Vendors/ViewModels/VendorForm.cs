using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;

namespace EskaCommerce.Vendors.Areas.Vendors.ViewModels
{
    public class VendorForm
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Slug { get; set; }

        [EmailAddress]
        [Required(ErrorMessage = "The {0} field is required.")]
        public string Email { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; }

        public long SiteId { get; set; }
        public Site Site { get; set; }

        public IList<VendorManager> Managers { get; set; } = new List<VendorManager>();
     
    }
}
