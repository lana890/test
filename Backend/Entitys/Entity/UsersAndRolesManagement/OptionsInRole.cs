using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static EskaCMS.Entitys.Enum.GeneralEnums;

namespace EskaCMS.Entitys.Entity.Core
{
    public partial class OptionsInRole : EntityBase
    {
        
        public long RoleId { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(150)]
        public string CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public string ModifiedBy { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public ERoleValue Value { get; set; }


        public virtual Role Role { get; set; }
    }
}
