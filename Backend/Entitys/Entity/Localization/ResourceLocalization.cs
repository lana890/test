using EskaCMS.Infrastructure.Models;
using EskaCommerce.Infrastructure.Models;
using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Entities.Entity.Core
{
    public class ResourceLocalization : EntityBase
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Key { get; set; }

        public string Value { get; set; }

        //[Required(ErrorMessage = "The {0} field is required.")]
        //public string CultureId { get; set; }

        //public virtual Culture Culture { get; set; }

        //public virtual Site Site { get; set; }
        //public long SiteId { get; set; }
        public long SiteCultureId { get; set; }
        public virtual SitesCultures SiteCulture { get; set; }
    }
}