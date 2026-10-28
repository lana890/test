
using EskaCMS.Infrastructure.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Entities.Entity.Core
{
    public class SitesHostNames : EntityBase
    {
        public long  SiteId { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(500)]
        public string HostName { get; set; }
         public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]

        public long CultureId { get; set; }
        public string Prefix { get; set; }
        public bool IsActive { get; set; }

        public string AddtionalMetaTags { get; set; }
        public long?    ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
         public virtual Site Site { get; set; }
        public virtual SitesCultures Culture { get; set; }
        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }
        public string PrimaryMetaTags { get; set; }



    }
}
