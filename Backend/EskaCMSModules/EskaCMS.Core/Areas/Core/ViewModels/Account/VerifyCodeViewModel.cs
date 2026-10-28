using System.ComponentModel.DataAnnotations;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Areas.Core.ViewModels.Account
{
    public class VerifyCodeViewModel
    {

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Code { get; set; }
        public string Username { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public string Purpose { get; set; }


    }
}
