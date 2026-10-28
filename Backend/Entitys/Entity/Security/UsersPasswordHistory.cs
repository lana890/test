
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Security
{
    public class UsersPasswordHistory :EntityBase
    {
        //change password
        public virtual EskaCMS.Entities.Entity.Core.UsersSites UsersSites { get; set; }
        public long UsersSitesId { get; set; }
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.ChangePasswordHistory ChangePasswordHistory { get; set; }
        public DateTimeOffset Date { get; set; }

  
    }
}
