using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Reviews
{
    public class ReviewProduct : EntityBase
    {
        public long ProductId { get; set; }
        public Product Product { get; set; }

        public long ReviewId { get; set; }

        public Review Review { get; set; }

        public long? MainProductId { get; set; }
        public Product MainProduct { get; set; }
    }
}
