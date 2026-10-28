using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.BlogArchive.Models
{
    public class PostAuthorVM
    {
        public long AuthorId { get; set; }
        public string Name { get; set; }
        public long PostId { get; set; }
        public long UserId { get; set; }
    }
}
