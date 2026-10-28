using EskaCMS.Entities.Entity.BlogArchive;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Reviews
{
    public class ReviewPosts : EntityBase
    {
        public long PostId { get; set; }
        public Posts Post { get; set; }

        public long ReviewId { get; set; }

        public Review Review { get; set; }
    }


}
