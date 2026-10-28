using EskaCMS.Core.BusinessModels;
using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.BusinessModels
{
    public class RegisterViewModel
    {

        [Required(ErrorMessage = "The {0} field is required.")]
        [Display(Name = "Username")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [Display(Name = "Name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 4)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
        public long? GroupId { get; set; }

        public List<UserOptionsVM> UserOptions { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public long RoleId { get; set; }
        public DateTimeOffset? DateOfBirth { get; set; }

        public bool IsEskaCoreIntegration { get; set; }
        public bool? EnableTwoFactorAuthentication { get; set; }
        public bool IsADAuthRequired { get; set; }

        public long? SiteId { get; set; } = null;

        public string ReferrerCode { get; set; }
        public EStatus Status { get; set; } = EStatus.Active;

        public string EmailTempleteId {  get; set; }
    }



    public class RegisterWithoutPasswordVM
    {

        [Required(ErrorMessage = "The {0} field is required.")]
        [Display(Name = "Username")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [Display(Name = "Name")]
        public string FullName { get; set; }

        public string PhoneNumber { get; set; }
        public long? GroupId { get; set; }

        public List<UserOptionsVM> UserOptions { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public long RoleId { get; set; }
        public DateTimeOffset? DateOfBirth { get; set; }

        public bool IsEskaCoreIntegration { get; set; }
        public bool IsADAuthRequired { get; set; }

        public long? SiteId { get; set; } = null;

        public EStatus Status { get; set; } = EStatus.Active;
    }


}
