using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCms.CoreShared.ViewModels
{
    public class UserFilteVM
    {
        
      
            public UserFilteVM()
            {
                CreationDate = DateTimeOffset.Now;
                ModificationDate = DateTimeOffset.Now;
                RequestStatus = ERequestStatus.Pending;
                UserSites = new HashSet<UsersSites>();
            }

            [Required(ErrorMessage = "The {0} field is required.")]
            public EUsersTypes Type { get; set; }

            public long CreatedById { get; set; }
            [Required(ErrorMessage = "The {0} field is required.")]
            [StringLength(450)]
            public string FullName { get; set; }
            public string PhoneNumber { get; set; }
            public bool IsADAuthRequired { get; set; }
            public DateTimeOffset CreationDate { get; set; }
            public long? ModifiedById { get; set; }
            public DateTimeOffset? ModificationDate { get; set; }

            public EStatus Status { get; set; }
            public DateTimeOffset StatusDate { get; set; }

            public ERequestStatus RequestStatus { get; set; }
            public DateTimeOffset RequestStatusDate { get; set; }
            [StringLength(450)]
            public string RefreshTokenHash { get; set; }

            public long? VendorId { get; set; }
            // public IList<UserAddress> UserAddresses { get; set; } = new List<UserAddress>();
            public IList<UserRole> UserRoles { get; set; } = new List<UserRole>();

            public IList<CustomerGroupUser> CustomerGroups { get; set; } = new List<CustomerGroupUser>();

            public long? ThumbnailId { get; set; }
            public virtual Media Thumbnail { get; set; }

        public bool IsLockout { get; set; }

        public const string SettingsDataKey = "";
            public string ExtensionData
            {
                get; set;
            }


            public string UserGuid { get; set; }
            //public UserAddress DefaultShippingAddress { get; set; }
            public long? DefaultShippingAddressId { get; set; }
            public DateTimeOffset? LastLoginDate { get; set; }
            public string DeviceToken { get; set; }
            public string Image { get; set; }
            public bool? IsNewsLetterUser { get; set; }
            //public string ConnectionId { get; set; }

            public string ClientIP { get; set; }
            public string Location { get; set; }
            public long? ParentId { get; set; }
            public User Parent { get; set; }

            public DateTimeOffset? DateOfBirth { get; set; }
            public ICollection<UserOptions> UserOptions { get; set; } = new List<UserOptions>();
            public long CustomerGroupId { get; set; }
            public long RoleId { get; set; }
            public string UserName { get; set; }
             public string Email { get; set; }
            public long UserId { get; set; }
        public ICollection<UsersSites> UserSites { get; set; }
        
    }

    public class EmployeeViewModel
    {
      
    }
    public class DepartmentEmployeeViewModel
    {
        public string DepartmentName { get; set; }
        public string DepartmentCode { get; set; }
        public long DepartmentId { get; set; }
        public string FullName { get; set; } // FL_NAME
        public string EmployeeRank { get; set; }
        public string Username { get; set; }
        public string CompanyEmail { get; set; }
        public string JobTitleName { get; set; }
        public string Mobile { get; set; }
        public string Phone2 { get; set; }
        public string Email { get; set; }
        public string Name2 { get; set; }
        public long? ManagerId { get; set; }
        public string EmployeeId { get; set; }
        public string PhotoPath { get; set; }
        public int? AvailabilityStatus { get;  set; }
        public DateTimeOffset? StartDate { get;  set; }
        public DateTimeOffset? DateOfBirth { get;  set; }
        public EStatus Status { get;  set; }
    }
    public class DepartmentJson
    {
        public int DepartmentId { get; set; }
    }
}
