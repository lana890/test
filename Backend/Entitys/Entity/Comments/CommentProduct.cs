using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Comments
{
    public class CommentProduct : EntityBase
    {
        public long ProductId { get; set; }

        public Product Product { get; set; }


        public long CommentId { get; set; }
        public Comment Comment { get; set; }
    }
}
