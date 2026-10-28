using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Engine.Models
{
    public class UserOptionsToBeAddedVM
    {
        public long UserId { get; set; }
        public string OptionName { get; set; }
        public string OptionValue { get; set; }
    }
}
