using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.DynamicForms.ViewModels
{
    public class CriteriaVM
    {
        public long Id { get; set; }


        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }


        public EDynamicFormsCriteriaTypes Type { get; set; }


        public EDynamicCriteriaOperations OperationType { get; set; }
        public EDynamicCriteriaConditions ConditionsType { get; set; }

        public EStatus Status { get; set; }

        public IList<long> SubCategoryId { get; set; } = new List<long>(); // Petrol / Hyprid .....


        [StringLength(450)]
        public string Value { get; set; }




        public long? ClassificationId { get; set; }


        public long? CategoryId { get; set; }
        public long? FormFieldId { get; set; }
    }
}
