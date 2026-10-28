using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static EskaCMS.Entitys.Enum.GeneralEnums;

namespace EskaCMS.Entitys.Entity.Core
{
    public  class Options :EntityBase
    {
      
        public Options()
        {
            //this.options_in_levels = new HashSet<options_in_levels>();
        }
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(500)]
        public string Name { get; set; }
        public string Name2 { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(30)]
        public string Code { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(150)]
        public string CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public string ModifiedBy { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public EDefinitionType TYPE { get; set; }
        public long CompanyId { get; set; }

    }
}
