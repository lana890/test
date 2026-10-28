using static EskaCMS.Entities.Enum.GeneralEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Forms.Model.FormsViewModel
{
    public class FormEditVM
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }
        public string Settings { get; set; }
        public string Keys { get; set; }

        public string ComponentSettingsId { get; set; }

        public Entities.Enum.Source Source { get; set; }
        public string AdditionalDate { get; set; }
    }
}
