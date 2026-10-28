using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Core
{
    public class SlugInfo:EntityBase
    {
        public long? SiteId { get; set; }
        public Site Site { get; set; }

        public long? CultureId { get; set; }
        public SitesCultures Culture { get; set; }

        [StringLength(500)]
        [Required(ErrorMessage = "The {0} field is required.")]
        public string Slug { get; set; }
        public long CreatedById { get; set; }

        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public User CreatedBy { get; set; }
        public User ModifiedBy { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.EEntity Entity { get; set; }

    }
}
