
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.DynamicForms
{
    public partial class Classifications : EntityBase
    {

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }

        public string Description { get; set; }

        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }

        //[Required(ErrorMessage = "The {0} field is required.")]
        public long? FormItemId { get; set; }
        public virtual FormItem FormItem { get; set; }


        public IList<Criteria> CriteriaList { get; set; }

        public bool IsGeneral { get; set; }

        public long CreatedById { get; set; }
        public virtual User CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }


        public long? ModifiedById { get; set; }
        public virtual User ModifiedBy { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }


        public long SiteId { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }


    }
}
