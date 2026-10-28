using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Pages
{
    public class FavouritePages:EntityBase
    {
        public long UserId { get; set; }
        public User User { get; set; }

        public long PageId { get; set; }
        public Page Page { get; set; }

        public long CreatedById { get; set; }
        public DateTime CreationDate { get; set; } = DateTime.Now;
    }
}
