using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Pages.Models
{
    public class SetLoginPageVM
    {
        public List<long> ParentsPageId { get; set; }
        public long? LoginPageId { get; set; }
    }
}
