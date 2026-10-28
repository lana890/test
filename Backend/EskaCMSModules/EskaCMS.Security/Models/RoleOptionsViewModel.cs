using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Security.Models
{
   public class RoleOptionsViewModel
    {
     
        public long RoleId { get; set; }
        public List<OptionsForRoles> lstOptions = new List<OptionsForRoles>();
    }
    public class OptionsForRoles
    {
        public string OptionCode { get; set; }
        public long OptionId { get; set; }
        public short OptionValue { get; set; }
        public long CategoryId { get; set; }
    }
}
