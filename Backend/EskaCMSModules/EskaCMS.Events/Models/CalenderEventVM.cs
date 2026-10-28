using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Events.Models
{
    public class CalendarNotificationModel
    {
        public string Title { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public IEnumerable<Attendee> Attendees { get; set; }
    }

    public class Attendee
    {
        public string AttendeeName { get; set; }
        public string AttendeeEmail { get; set; }
    }
   
}
