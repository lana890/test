using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Sites
{
    public partial class Aliases :EntityBase
    {
       // public int Id { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string AliasUrl { get; set; }
        public string RedirectUrl { get; set; }

      
        public long CreatedById { get; set; }
        public DateTime CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTime? ModificationDate { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public long SiteId { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }
        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }
    }
}
