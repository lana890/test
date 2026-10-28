using System.ComponentModel.DataAnnotations;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.OTP.Models
{
    // Same contracts as EskaCMS.Core / EskaCMS.Security so clients get identical request/response shapes.

    public class LoginViewModel
    {
        public LoginViewModel()
        {
            LoginType = LoginTypes.UserName;
        }

        [Required(ErrorMessage = "The Username field is required.")]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public IList<long?> AuthorizedGroups { get; set; }

        public LoginTypes LoginType { get; set; }
        public string AdditionalInfoValue { get; set; }

        public bool FromGoogleOrFacebookOrApple { get; set; } = false;
        public bool ValidateFromAd { get; set; } = false;
    }

    public class LoginResponse
    {
        public TokenResponse Token { get; set; }
        public string Culture { get; set; }
        public IList<UserOptionsVM> UserAdditionData { get; set; }
        public string Email { get; set; }
        public string ImageUrl { get; set; }
        public IList<UserRoleVM> Role { get; set; }
    }

    public class TokenResponse
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiryDate { get; set; }
    }

    public class UserOptionsVM
    {
        public string PropertyName { get; set; }
        public string Value { get; set; }
        public EControlTypes ControlType { get; set; }
        public bool IsEditable { get; set; }
        public bool IsVisible { get; set; }
        public long Id { get; set; }
    }

    public class UserRoleVM
    {
        public long RoleId { get; set; }
        public string RoleName { get; set; }
    }

    public class loginRequestAdViewModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
