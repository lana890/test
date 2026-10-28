
using EskaCMS.Infrastructure.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Core

{

    public partial class Assets : EntityBase
    {
        public Assets()
        {
            CreationDate = DateTimeOffset.Now;
        }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Name { get; set; }

        public string Description { get; set; }
        public string Content { get; set; }


        public EAssetsType Type { get; set; }
        

        public DateTimeOffset CreationDate { get; set; }


        public DateTimeOffset? ModificationDate { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public EStatus Status { get; set; }


        [Required(ErrorMessage = "The {0} field is required.")]
        public long SiteId { get; set; }
        public virtual Site Site { get; set; }


        public long CreatedById { get; set; }
        public virtual User CreatedBy { get; set; }

        public long? ModifiedById { get; set; }

        public virtual User ModifiedBy { get; set; }

    }
}
