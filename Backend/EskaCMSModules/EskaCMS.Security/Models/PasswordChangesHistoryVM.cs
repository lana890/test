using static EskaCMS.Entities.Enum.GeneralEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Models
{
    public class PasswordChangesHistoryVM
    {
        public DateTimeOffset Date { get; set; }
        public ChangePasswordHistory ChangeReason { get; set; }
    }
}
