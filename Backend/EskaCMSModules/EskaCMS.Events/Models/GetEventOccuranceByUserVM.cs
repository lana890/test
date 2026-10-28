using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Events.Models
{
    public class GetEventOccuranceByUserVM
    {
        public long? UserId { get; set; }
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }

    }

    public class GetEventOccuranceByUserDto
    {
        public DateTimeOffset ReservationDate { get; set; }

        public List<GetEventOccuranceByUserListDto> Events { get; set; }

    }

    public class GetEventOccuranceByUserListDto
    {
   
        public long Id { get; set; }
        public string Name { get; set; }
        public string CoreUsername { get; set; }
        public string Description { get; set; }
        public DateTimeOffset FromTime { get; set; }
        public DateTimeOffset ToTime { get; set; }
        public EventOccurrenceAtendeesVM? EventOccurrenceAtendees { get; set; }


    }
}
