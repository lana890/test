using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Entities.Entity.NewsLetter;
using EskaCMS.Entities.Entity.UsersAndRolesManagement;
using EskaCMS.Infrastructure.Models;
    
namespace EskaCMS.Entities.Entity.Core
{
    public class CustomerGroup : EntityBase
    {
        public CustomerGroup()
        {
            CreatedOn = DateTimeOffset.Now;
            LatestUpdatedOn = DateTimeOffset.Now; 
        }

       
        public string AdditionalInfo { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset LatestUpdatedOn { get; set; }
        public IList<CustomerGroupUser> Users { get; set; } = new List<CustomerGroupUser>();
        public IList<CustomerGroupDetails>  customerGroupDetails { get; set; } = new List<CustomerGroupDetails>();
        public string FireBaseTopic { get; set; }
        public long? SiteId { get; set; }
        public EskaCMS.Entities.Entity.Core.Site Site { get; set; }
        
        public long? GroupTypeId { get; set; }
        public CustomerGroupType GroupType { get; set; }

        public int PasswordLoginAttempts { get; set; }
        public int MaxSessionsNumber { get; set; }

        public int PasswordExpiryInDays { get; set; } 
        public TimeSpan LockoutDuration { get; set; }
        public string? Email { get; set; }

        public bool EnableTwoFactorAuthentication { get; set; }

        public long? ThumbnailId { get; set; }
        public virtual Media Thumbnail { get; set; }
        public List<NewsLetterGroupSubscribers> SubscribedNewsLetters { get; set; }
    }
}
