using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Core.BusinessModels
{
   public class RoleClaimVM
    {
        public string RoleId { get; set; }
        public List<string> ClaimValues { get; set; }
    }
}
