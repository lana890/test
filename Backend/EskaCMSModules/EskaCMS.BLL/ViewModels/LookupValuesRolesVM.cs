using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.General.ViewModels
{
    public class LookupValuesRolesVM
    {
        public long LookupValueId { get; set; }
        public List<long> assingedRolesIds { get; set; }
    }
}
