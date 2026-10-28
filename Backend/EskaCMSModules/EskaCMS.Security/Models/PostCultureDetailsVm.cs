using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Models
{
    public class PostCultureDetailsVm
    {
        public long? CultureId { get; set; }
        public string CultureCode { get; set; }
        public string Slug { get; set; }
        public long? ParentId { get; set; }
    
    }
}
