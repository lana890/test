using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Chat
{
    public class Group : EntityBaseWithTypedId<string>
    {
        public Group(string id,DateTimeOffset creationDate)
        {
            Id = id;
            CreationDate = creationDate;
        }
        public DateTimeOffset CreationDate { get; set; }
        public ICollection<Connection> Connections { get; set; }=new List<Connection>();
        [NotMapped]
        public long? RecipentId { get; set; }

    }
}
