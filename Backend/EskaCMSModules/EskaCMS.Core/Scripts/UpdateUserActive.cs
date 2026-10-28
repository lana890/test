using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Scripts
{
    public class UpdateUserActive
    {
        public static string UpdateUser = @"update core_user  set Status  = 5 where Status=0";
        public static string CustomergroupScript = @"Update core_customergroup  Set PasswordExpiryInDays=30,MaxSessionsNumber=10 Where MaxSessionsNumber=0 or PasswordExpiryInDays=0";

    }
}
