using System;
using EskaCMS.Infrastructure.Models;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.ProductComparison
{
    public class ComparingProduct : EntityBase
    {
        public DateTimeOffset CreatedOn { get; set; }

        public long UserId { get; set; }

        public User User { get; set; }

        public long ProductId { get; set; }

        public Product Product { get; set; }
    }
}
