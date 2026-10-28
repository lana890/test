using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Security.Models
{
    public class RestrictedPasswordsVM
    {
        public long Id { get; set; }
        public EStatus Status { get; set; }


        public string RestrictedText { get; set; }
    }
}
