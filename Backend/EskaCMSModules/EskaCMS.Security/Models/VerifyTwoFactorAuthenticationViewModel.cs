using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Models
{
    public class VerifyTwoFactorAuthenticationViewModel
    {

        public string UserName { get; set; }

        public string Code { get; set; }
        public string purpose { get; set; }
    }
}
