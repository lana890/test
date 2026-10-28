using EskaCMS.Core.Areas.Core.ViewModels.GroupTypeModels.Response;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Areas.Core.ViewModels.Shared
{
    public class CustomerGroupVM
    {
        public long Id { get; set; }
        public string Topic { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public bool IsActive { get; set; }
        public bool? EnableTwoFactorAuthentication { get; set; }

        public string Email { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public int MaxSessions { get; set; }
        public int PasswordExpiryInDays { get; set; }

        public long? ThumbnailId { get; set; }
        public string ImageUrl { get; set; }
        public string AdditionalInfo { get; set; }

        public GroupTypeGridVM GroupType { get; set; }

        public List<GetCustomerGroupDetailsForm> customerGroupDetails { get; set; } = new List<GetCustomerGroupDetailsForm>();

    }
    public class GetCustomerGroupDetailsForm
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public long CultureId { get; set; }
        public string CultureCode { get;  set; }
    }
}
