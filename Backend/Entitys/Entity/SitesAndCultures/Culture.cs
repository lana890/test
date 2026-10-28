using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Infrastructure.Models;

namespace EskaCMS.Entities.Entity.Core
{
    public class Culture : EntityBaseWithTypedId<string>
    {
        public Culture(string id)
        {
            Id = id;
        }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }

        public virtual ICollection<EskaCMS.Entities.Entity.Core.SitesCultures> SitesCultures { get; set; }
    }
}
