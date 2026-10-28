using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.DynamicForms
{
    public partial class Criteria: EntityBase
    {

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }


        public EskaCMS.Entities.Enum.GeneralEnums.EDynamicFormsCriteriaTypes Type { get; set; }


        public EskaCMS.Entities.Enum.GeneralEnums.EDynamicCriteriaOperations OperationType { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.EDynamicCriteriaConditions ConditionsType { get; set; }

        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }


        //[Required(ErrorMessage = "The {0} field is required.")]

        public long? ClassificationId { get; set; }
        public virtual Classifications Classification { get; set; }

        public bool IsGeneral { get; set; }

        [StringLength(450)]
        public string Value { get; set; }



        public long? CategoryId { get; set; } // Engine ...
        public Category Category { get; set; }




        public long? FormFieldId { get; set; } 
        public FormField FormField { get; set; }

        public long CreatedById { get; set; }
        public virtual User CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }

        public IList<CategoryCriteria>? CategoryCriterias { get; set; }

        public long? ModifiedById { get; set; }
        public virtual User ModifiedBy { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }


        public long SiteId { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }



    }
}
