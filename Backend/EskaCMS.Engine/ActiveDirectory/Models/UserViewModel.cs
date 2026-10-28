using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Engine.Models
{
    public class UserViewModel
    {
        public string UserName { get; set; }
        public long Id { get; set; }
        public string FullName { get; set; }
        public string NormalizedUserName { get; set; }
        public string Email { get; set; }
        public string NormalizedEmail { get; set; }
        public long? ParentId { get; set; }
        public int Status { get; set; }
        public IList<long>? RoleIds { get; set; } = new List<long>();
        public IList<long>? CustomerGroupIds { get; set; } = new List<long>();
    }
    public class UserOptionsVM
    {
        public string PropertyName { get; set; }
        public string Value { get; set; }
        public int ControlType { get; set; }
        public bool IsEditable { get; set; }
        public bool IsVisible { get; set; }
        public long Id { get; set; }
    }
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

        public long RoleId { get; set; }
        public DateTimeOffset? DateOfBirth { get; set; }
        public List<UserOptionsVM> UserOptions { get; set; }

        public bool IsEskaCoreIntegration { get; set; }
        public bool? EnableTwoFactorAuthentication { get; set; }
        public bool IsADAuthRequired { get; set; }

        public long? SiteId { get; set; } = null;

        public string ReferrerCode { get; set; }
        public int Status { get; set; } = 5;
        public string SerializeObject()
        {
            return JsonConvert.SerializeObject(this);
        }

    }

    public class DCMSRegisterResponse<T>
    {
        public T content { get; set; }
        public string code { get; set; }
        public string message { get; set; }

    }

    public class RegisterationResultDTO
    {
        public string UserId { get; set; }
        public string Message { get; set; }

        public RegisterationResultDTO(string UserId, string Message)
        {
            this.UserId = UserId;
            this.Message = Message;

        }
    }


    public class RegisterationResponseDTO
    {
        public long UserId { get; set; }
        public string Message { get; set; }

        public RegisterationResponseDTO(long UserId, string Message)
        {
            this.UserId = UserId;
            this.Message = Message;

        }
    }
    public class CultureViewModel
    {
        public string CultureCode { get; set; }
        public long CultureId { get; set; }

    }

}
