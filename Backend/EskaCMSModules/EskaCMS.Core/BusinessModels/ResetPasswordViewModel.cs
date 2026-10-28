using static EskaCMS.Entities.Enum.GeneralEnums;
using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Core.Entities
{
    public class ResetPasswordViewModel
    {
        //[Required(ErrorMessage = "The {0} field is required.")]

        public long UserId { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
        public string Username { get; set; }
        public string Purpose { get; set; }

        public bool IsResetPassword { get; set; }
      

    }


}
