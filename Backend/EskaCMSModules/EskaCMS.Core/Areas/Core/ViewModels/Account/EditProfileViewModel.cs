using EskaCMS.Core.BusinessModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EskaCMS.Core.Areas.Core.ViewModels.Account
{
    public class EditProfileViewModel
    {
        //public long Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [EmailAddress]
        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public string Image { get; set; }

        public List<UserOptionsVM> UserOptions { get; set; }
        //public long UserId { get; set; }

        public DateTimeOffset? DateOfBirth { get; set; }

        public bool? EnableTwoFactorAuthentication { get; set; }
    }
}
