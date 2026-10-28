using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Models
{
  public  class UserInfoClaimsVM
    {

        public long UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string MobileNumber { get; set; }
        public string[] RoleId { get; set; }
        public DateTimeOffset?    DateofBirth { get; set; }
        public string Username { get; set; }
        public EUsersTypes UserType { get; set; }
        public string DefaultShippingAddressId { get; set; }
        public long? CurrencyCultureId { get; set; }

        public long? CultureId { get; set; }

    }
}
