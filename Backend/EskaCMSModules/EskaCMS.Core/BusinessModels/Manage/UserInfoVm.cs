using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Core.Manage
{
    public class UserInfoVm
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Email { get; set; }
    }
}
