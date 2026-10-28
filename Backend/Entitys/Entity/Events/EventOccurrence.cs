using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace EskaCMS.Entities.Entity.Events
{
  public  class EventOccurrence : EntityBase
    {

        public string Name { get; set; }
        public string Description { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }
        public long SiteId { get; set; }
        public DateTimeOffset ReservationDate { get; set; }
        public DateTimeOffset FromTime { get; set; }
        public DateTimeOffset ToTime { get; set; }

        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }
        public Media Thumbnail { get; set; }

        public long? ThumbnailId { get; set; }
        public EventLocation Location { get; set; }
        public long LocationId { get; set; }
        public Event Event { get; set; }
        public long? EventId { get; set; }
        public long MaxEnrollment { get; set; }

        public long CreatedById { get; set; }
        public virtual User CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public virtual User ModifiedBy { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public long? ModifiedById { get; set; }

        public string Settings { get; set; }

        public List<EventAttendees> EventAttendees { get; set; }


    }
}
