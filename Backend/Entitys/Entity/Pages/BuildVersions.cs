using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Entities.Entity.Core
{
    public partial class BuildVersions :EntityBase
    {
       // public int Id { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string FileName { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        public EskaCMS.Entities.Enum.GeneralEnums.EComponentType ComponentType { get; set; }
      
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }

        public virtual User CreatedBy { get; set; }
  


    }
}
