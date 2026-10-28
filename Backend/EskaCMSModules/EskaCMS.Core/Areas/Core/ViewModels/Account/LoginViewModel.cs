using EskaCMS.Core.BusinessModels;
using EskaCMS.Entities.Entity.Core;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static EskaCMS.Core.Models.TokenVM;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Areas.Core.ViewModels.Account
{
    public class LoginViewModel
    {

        public LoginViewModel()
        {
            LoginType = LoginTypes.UserName;
        }
        [Required(ErrorMessage = "The Username field is required.")]
        //[EmailAddress(ErrorMessage = "The Username field is not a valid Username.")]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public IList<long?> AuthorizedGroups { get; set; }

       // public bool ExcludeUserRolesOptions { get; set; }

        public LoginTypes LoginType { get; set; }
        public string AdditionalInfoValue { get; set; }


        public bool FromGoogleOrFacebookOrApple { get; set; } = false;
        public bool ValidateFromAd  { get; set; } = false;

    }

    
}
