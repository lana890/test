using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.NewsLetter
{
    public class NewsLetterGroupSubscribers : EntityBase
    {
        public long NewsLetterId { get; set; }
        public virtual NewsLetters NewsLetter { get; set; }

        public long CustomerGroupId { get; set; }
        public virtual CustomerGroup CustomerGroup { get; set; }
    }
}
