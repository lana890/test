using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.DynamicForms.ViewModels
{
    public class ClassificationVM
    {
        public long Id { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }

        public string Description { get; set; }

        public EStatus Status { get; set; }

        public long? FormItemId { get; set; }

        public bool? IsGeneral { get; set; }
        public IList<CriteriaVM> CriteriaList { get; set; }=new List<CriteriaVM>();

    
    }
    public class FormItemsClassificationVM
    {
        public long Id { get; set; }
       
        public string Name { get; set; }

        public int CriteriaCount { get; set; }
         


    }
    
}
