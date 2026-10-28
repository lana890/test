

using EskaCMS.Entities.Entity.Achievements;
using EskaCMS.Entities.Entity.Chat;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.UsersAndRolesManagement;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Core
{
    [Index(nameof(UserName), Name = "UserNameidx")]
    [Index(nameof(PhoneNumber), Name = "PhoneNumberidx")]
    [Index(nameof(Email), Name = "Emailidx")]
    [Index(nameof(PhoneNumber), nameof(UserName), nameof(Email), Name = "PhoneNumberwithUserNameidx")]
    [Index(nameof(PhoneNumber), nameof(NormalizedEmail), nameof(NormalizedUserName), Name = "PhoneNumberwithNormalizedUserNameidx")]

    public class User : IdentityUser<long>, IEntityWithTypedId<long>, IExtendableObject
    {
        public User()
        { 
            CreationDate = DateTimeOffset.Now;
            RequestStatus = ERequestStatus.Pending;
            RequestStatusDate = DateTimeOffset.Now;
            StatusDate = DateTimeOffset.Now;
            UserSites = new HashSet<UsersSites>();
            NewsLetterSubscribers = new HashSet<NewsLetterSubscribers>();
            UserOTP = new HashSet<UserOTP>();

        }


        [Required(ErrorMessage = "The {0} field is required.")]
        public EUsersTypes Type { get; set; }

        public long CreatedById { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string FullName { get; set; }

        public bool IsADAuthRequired { get; set; }
        public bool IsFacebookRegistered { get; set; }
        public bool IsGoogleRegistered { get; set; }
        public bool IsAppleRegistered { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }

        public EStatus Status { get; set; }
        public DateTimeOffset StatusDate { get; set; }

        public ERequestStatus RequestStatus { get; set; }
        public DateTimeOffset RequestStatusDate { get; set; }
        public long? VendorId { get; set; }
        // public IList<UserAddress> UserAddresses { get; set; } = new List<UserAddress>();
        public IList<UserRole> UserRoles { get; set; } = new List<UserRole>();

        public IList<CustomerGroupUser> CustomerGroups { get; set; } = new List<CustomerGroupUser>();

        [ForeignKey(name: "ThumbnailId")]
        public long? ThumbnailId { get; set; }
        public virtual Media Thumbnail { get; set; }


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
        //public string ConnectionId { get; set; }

        public string ClientIP { get; set; }
        public string Location { get; set; }
        public long? ParentId { get; set; }
        public User Parent { get; set; }
        
        public long? ReferrerId { get; set; }
        public User Referrer { get; set; }
        public ICollection<User> RefereeUsers { get; set; }
        public DateTimeOffset? DateOfBirth { get; set; }
        public ICollection<UserOptions> UserOptions { get; set; } = new List<UserOptions>();

        public ICollection<UsersSites> UserSites { get; set; }

        public long? DefaultCultureId { get; set; }

        public ICollection<NewsLetterSubscribers> NewsLetterSubscribers { get; set; }
        public List<Message> MessagesSent { get; set; }
        public List<Message> MessagesReceived { get; set; }
        public virtual SitesCultures DefaultCulture { get; set; }
        public long? CurrencyCultureId { get; set; }
        public CurrencyRates CurrencyCulture
        {
            get; set;
        }
        public long? FavoritePageId { get; set; }
        public string Code { get; set; }
        public DateTime? lastPasswordUpdate { get; set; }

        public ICollection<UserOTP> UserOTP { get; set; }

        //UserFiles for exkaMedica
        public ICollection<UserFiles> UserFiles { get; set; }
        public ICollection<UserTasks> UserTasks { get; set; }
        public ICollection<Friendships> InitiatedFriendships { get; set; }
        public ICollection<Friendships> ReceivedFriendships { get; set; }
        public ICollection<UserGeoLocation> UserGeoLocation { get; set; }
        public ICollection<UserAchievments> UserAchievments { get; set; }

        public string FingerSign { get; set; }

        public EPlatformSource Channel { get; set; }


    }
}
