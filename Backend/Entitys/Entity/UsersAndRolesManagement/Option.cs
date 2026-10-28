
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.Core
{
    public class Option :EntityBase
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(50)]
        public string Code { get; set; }
        public User CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public User ModifiedBy { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public EDefinitionType Type { get; set; }
        public EskaCMS.Entities.Entity.Core.Site Site { get; set; }
        public long? SiteId { get; set; }
        public long CreatedById { get; set; }
        public long? ModifiedById { get; set; }

        public Category Category { get; set; }
        public long CategoryId { get; set; }

        public ICollection<Options_in_Role> RoleOptions { get; set; }
    }
}
