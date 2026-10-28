using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Events.Models
{
  public  class EventOccurrenceVM
    {


        public long Id { get; set; }
        public string Name { get; set; }
        public string CoreUsername { get; set; }
        public string Description { get; set; }
        public long SiteId { get; set; }

        public DateTimeOffset ReservationDate { get; set; }
        public DateTimeOffset FromTime { get; set; }
        public DateTimeOffset ToTime { get; set; }

        public EStatus Status { get; set; }
        public Media Thumbnail { get; set; }

        public long? ThumbnailId { get; set; }
       
        public long LocationId { get; set; }
      
        public long? EventId { get; set; }
        public long MaxEnrollment { get; set; }

        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
       
        public DateTimeOffset? ModificationDate { get; set; }
        public long? ModifiedById { get; set; }

        public string Settings { get; set; }

        public EventOccurrenceAtendeesVM? EventOccurrenceAtendees { get; set; }
        public string FullName { get; set; }
        public string DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string Email { get;  set; }
    }


    public class EventOccurrenceAtendeesVM
    {

        public List<string> ExternalAtendees { get; set; }
        public List<long> InternalAtendees { get; set; }
        //public List<string> InternalEmails { get; set; }
        public List<InternalAttendeesDto> InternalUsers { get; set; }


    }


    public class InternalAttendeesDto
    {
        public long Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string DepartmentId { get; set; }
        public string DepartmentName { get; set; }
    }
}
