using EskaCMS.Entities.Entity.Shared;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Core
{
    public partial class Tags:EntityBase
    {
        public long SiteId { get; set; }
        [Required(ErrorMessage = "The Title field is required.")]
        [StringLength(1000)]
        public string Title { get; set; }
     
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }

        public virtual Site   Site { get; set; }
        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }

        public List<EntityTags> EntityTags { get; set; }

    }
}
