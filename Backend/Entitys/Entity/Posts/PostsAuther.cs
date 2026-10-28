using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.BlogArchive
{
    public class PostsAuther: EntityBase
    {
        public long userId { get; set; }
        public User user { get; set; }
        public long postId { get; set; }
        public Posts post { get; set; }
    }
}
