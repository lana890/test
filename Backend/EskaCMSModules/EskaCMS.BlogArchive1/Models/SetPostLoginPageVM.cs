using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.BlogArchive.Models
{
    public class SetPostLoginPageVM
    {
        public List<long> ParentsPostId { get; set; }
        public long? LoginPageId { get; set; }
    }
}
