using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.ADIntegration.ViewModels
{
    public class SendCodeIntegerationViewModel
    {
        public int InvokerId { get; set; }
        //public dynamic InvokerObj { get; set; }
        public string Username { get; set; }
        public E2FAProviders SelectedProvider { get; set; }

        public string Value { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Purpose { get; set; }
        public int? WorkFlowId { get; set; } = 13715;
        public int? ContractId { get; set; } = 100002948;
        public EIntegerationType IntegerationType { get; set; }
    }
}
