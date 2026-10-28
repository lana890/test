using static EskaCMS.Entities.Enum.GeneralEnums;
using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Core.Manage
{
    public class ChangePasswordViewModel
    {

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Username { get; set; }

        //[Required(ErrorMessage = "The {0} field is required.")]
        // commented by israa for use in reset password in Otp proccess in front end 
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
        public ChangePasswordHistory ReasonChangStatus { get; set; }


    }
}
