using EskaCMS.Entities.Entity.BlogArchive;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Comments
{
    public class CommentPost :EntityBase
    {
        public long PostId { get; set; }

        public Posts Post { get; set; }


        public long CommentId { get; set; }
        public Comment Comment { get; set; }
    }
}
