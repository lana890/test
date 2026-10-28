using System.ComponentModel.DataAnnotations;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Module.Core.Areas.Core.ViewModels.Manage
{
    public class AddPhoneNumberViewModel
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        [Phone]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public SMSProviders SMSProvider { get; set; }
    }
}
