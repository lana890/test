using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Core.Entities
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Host { get; set; }
        public string EmailTemplateId { get; set; }


    }
}
