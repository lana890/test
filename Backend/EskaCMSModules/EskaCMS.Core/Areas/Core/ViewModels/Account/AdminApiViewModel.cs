using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EskaCMS.Core.Areas.Core.ViewModels.Account
{
    public class AdminApiViewModel
    {


        [Required(ErrorMessage = "The Username field is required.")]
        //[EmailAddress(ErrorMessage = "The Username field is not a valid Username.")]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
        public IList<long?> AuthorizedGroups { get; set; }
        //public bool ExcludeUserRolesOptions { get; set; }
      

    }
}
