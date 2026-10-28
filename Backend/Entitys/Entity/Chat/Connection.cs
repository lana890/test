using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Chat
{
    public class Connection :EntityBaseWithTypedId<string>
    {
        public Connection(string id ,long userId, DateTimeOffset creationDate)
        {
            Id = id;
            UserId = userId;
            CreationDate = creationDate;
        }

        public DateTimeOffset CreationDate { get; set; }
        public long UserId { get; set; }
        public User User { get; set; }
        [NotMapped]
        public long? RecipentId { get; set; }
    }
}
