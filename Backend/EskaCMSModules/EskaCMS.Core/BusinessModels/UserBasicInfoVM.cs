using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.BusinessModels
{
    public class UserBasicInfoVM
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }=null;
        public DateTimeOffset? DateOfBirth { get; set; }
    }

    public class FilterUserVM
    {
        public long? DepatmentId { get; set; }
        public string Username { get; set; }
        public int Pagesize { get; set; }
        public int Pagenumber { get; set; }
        public ESearchType? SearchType { get; set; }
        public int? Duration { get; set; }
    }
}
