using EskaCMS.Infrastructure.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace EskaCMS.Entities.Entity.Core
{
    public class CustomerGroupUser
    {
        public long UserId { get; set; }

        public virtual User User { get; set; }

        public long CustomerGroupId { get; set; }

        public virtual CustomerGroup CustomerGroup { get; set; }

        public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.Now;
    }
}
