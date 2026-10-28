using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Models
{
    public class GetRetrictedResponseVm
    {

        public List<RestrictedPasswordsVM> Data { get; set; }
        public int TotalPasswordsRestricted { get; set; }
    }
}
