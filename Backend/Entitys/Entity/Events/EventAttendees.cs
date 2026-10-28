using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Events
{
    public class EventAttendees : EntityBase
    {

        public string Email { get; set; }
        public long? UserId { get; set; }
        public User User{ get; set; }
        public long EventOccurrenceId { get; set; }
        public EventOccurrence EventOccurrence { get; set; }

    }
}
