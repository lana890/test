using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class CustomerGroupForm
    {
        public long? Id { get; set; }
        public List<CustomerGroupDetailsForm> customerGroupDetails { get; set; }

        
        public string Name { get; set; }
        public string Description { get; set; }

        public bool IsActive { get; set; }
        public string FireBaseTopic { get; set; }
        public string Email { get; set; }
        public int MaxSessions { get; set; }
        public int PasswordExpiryInDays { get; set; }
        public bool? EnableTwoFactorAuthentication { get; set; }
        public long? GroupTypeId { get; set; }
        public long? ThumbnailId { get; set; }
        public string ImageUrl { get; set; }
    }

    public class CustomerGroupDetailsForm
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public long CultureId { get; set; }
        public string CultureCode { get;  set; }
    }



}
