using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Core.Areas.Core.ViewModels.Account
{
    public class FacebookLoginViewModel
    {
        public string AccessToken { get; set; }

        [Required]
        public long CustomerGroupId { get; set; }

        [Required]
        public long RoleId { get; set; }
    }
}
