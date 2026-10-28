using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Security.Models
{
    public class GetPasswordHistoryVM
    {
        public ChangePasswordHistory EChangePasswordReason { get; set; }
        public DateTimeOffset ChagneDate { get; set; }
    }
}
