using EskaCMS.EskaCoreIntegration.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.BusinessModels
{
    public class UserForm
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string FullName { get; set; }

        public string CoreToken { get; set; }
        public long? VendorId { get; set; }
        public long? DefaultShippingAddressId { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [EmailAddress]
        public string Email { get; set; }
        public string UserName { get; set; }

        public string PhoneNumber { get; set; }

        public string Password { get; set; }
        public string Image { get; set; }
        public long? ParentId { get; set; }

        public IList<long> RoleIds { get; set; } = new List<long>();

        public long? ThumbnailId { get; set; }

        public IList<long> CustomerGroupIds { get; set; } = new List<long>();

        public List<UserOptionsVM> UserOptions { get; set; } = new List<UserOptionsVM>();
        public IList<long> AssignedSites { get; set; } = new List<long>();
        public long UserId { get; set; }

        public long CreatedById { get; set; }
        public string? Modified_by { get; set; }
        public bool IsADAuthRequired { get; set; }

        public int MyProperty { get; set; }
        public List<int>? CoreGroupsIds { get; set; } = null;
        public int[]? DeletedCoreGroupsIds { get; set; } = null;
        public int[] InsertCoreGroupsIds { get; set; }

        public string? BirthDate { get; set; }
        public EUsersTypes UserType { get; set; }
        public EStatus Status { get; set; }

        public bool? EnableTwoFactorAuthentication { get; set; }
        public IFormFile? UserImage { get; set; }
        public string ReferrerCode { get; set; }
    }
    public class UserAddress
    {
        public string AddressLine { get; set; }
        public long Latitude { get; set; }
        public long Longitude { get; set; }
    }

    public class UserDetailsVM
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string FullName { get; set; }

        public string CoreToken { get; set; }
        public long? VendorId { get; set; }
        public long? DefaultShippingAddressId { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [EmailAddress]
        public string Email { get; set; }
        public string UserName { get; set; }

        public string PhoneNumber { get; set; }

        public string Password { get; set; }
        public string Image { get; set; }

        public IList<long> RoleIds { get; set; } = new List<long>();

        public long? ThumbnailId { get; set; }

        public IList<long> CustomerGroupIds { get; set; } = new List<long>();

        public List<UserOptionsVM> UserOptions { get; set; } = new List<UserOptionsVM>();
        public IList<long> AssignedSites { get; set; } = new List<long>();
        public long UserId { get; set; }

        public long CreatedById { get; set; }
        public string? Modified_by { get; set; }
        public bool IsADAuthRequired { get; set; }

        public int MyProperty { get; set; }
        public List<CoreGroupResponseVM>? CoreGroupsIds { get; set; } = null;
        public int[]? DeletedCoreGroupsIds { get; set; } = null;
        public int[] InsertCoreGroupsIds { get; set; }

        public string? BirthDate { get; set; }
        public EUsersTypes UserType { get; set; }
        public EStatus Status { get; set; }

        public bool? EnableTwoFactorAuthentication { get; set; }
        public IFormFile? UserImage { get; set; }
        public string ReferrerCode { get; set; }

        public DateTimeOffset? CreationDate { get; set; }
    }
}
