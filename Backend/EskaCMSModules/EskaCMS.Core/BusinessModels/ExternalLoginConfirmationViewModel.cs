using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Core.Entities
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [Display(Name = "Name")]
        public string FullName { get; set; }
    }
}
