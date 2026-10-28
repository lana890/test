using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class UserWishList:EntityBase
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public User User { get; set; }
        public long SiteId { get; set; }
        public long ProductId { get; set; }
        public Product Product { get; set; }
        public Enum.GeneralEnums.EStatus Status { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }

    }
}
