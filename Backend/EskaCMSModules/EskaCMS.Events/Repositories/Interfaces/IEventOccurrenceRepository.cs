using EskaCMS.Events.Models;
using EskaCMS.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Events.Repositories.Interfaces
{
    public interface IEventOccurrenceRepository : IBaseRepository<EventOccurrenceVM,long>
    {
        Task<EventOccurrenceVM> GetEventOccuranceByEventId(long EventId, EStatus? Status);
        Task<List<EventOccurrenceVM>> GetEventOccuranceByDate(DateTimeOffset ReservationDate, long? LocationId, EStatus? Status = null);
        Task<List<EventOccurrenceVM>> GetEventOccuranceByLocationId(long LocationId, EStatus? Status = null);
        Task<bool> EditEventOccurrenceAtendees(long occurrenceId, EventOccurrenceAtendeesVM model);
        Task<List<GetEventOccuranceByUserDto>> GetEventOccuranceByUser(GetEventOccuranceByUserVM model);
        Task<bool> HandleEmailMeetings(long eventOccurrenceId);


    }
}
