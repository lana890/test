using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Models;
using EskaCMS.Events.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Events.Services.Interfaces
{
    public interface IEventsService
    {

        Task<List<EventVM>> GetAllEvents(string Name, string Description, DateTimeOffset? FromDate, DateTimeOffset? ToDate, DateTimeOffset? FromTime, DateTimeOffset? ToTime, long? LocationId, long? CultureId, long? CategoryId, EStatus? Status, long? ParentId);
        Task<EventVM> GetEventById(long EventId);
        Task<List<EventVM>> GetEventByCategoryId(long CatId);
        Task<List<CategoryViewModel>> GetCategoriesByTypeId();
        Task<bool> ChangeCategoryName(long CatId, CategoryViewModel CategoryVM);
        Task<long> AddCategory(long catId, CategoryViewModel category);
        Task<long> AddEvent(List<EventVM> EventVM);
        Task<bool> DeleteCategory(long catId);
        Task<bool> EditEvent(List<EventVM> EventVM, long EventId);
        Task<bool> ToggleStatus(long EventId, EStatus EventStatus);


        Task<long> AddLocation(EventLocationVM Location);
        Task<bool> EditLocation(EventLocationVM LocationVM, long LocationId);
        Task<List<EventLocationVM>> GetLocationsBySiteId();
        Task<EventLocationVM> GetLocationsById(long LocationId);
        Task<bool> ToggleLocationStatus(long LocationId, EStatus LocationStatus);
        Task<bool> DeleteLocation(long Id);

        Task<ValidReservationVM> AddEventOccurrence(EventOccurrenceVM OccurrenceVM);
        Task DeleteEventOccurrence(long ReservationId);
        Task<bool> EditEventOccurrence(EventOccurrenceVM OccurrenceVM, long OccurrenceId);

        Task<EventOccurrenceVM> GetEventOccuranceById(long EventOccuranceId);
        Task<EventOccurrenceVM> GetEventOccuranceEventId(long EventId, EStatus? Status);
        Task<List<EventOccurrenceVM>> GetEventOccuranceByDate(DateTimeOffset ReservationDate,long? LocationId, EStatus? Status);

        Task<List<EventOccurrenceVM>> GetEventOccuranceLocationId(long LocationId, EStatus? Status);

        Task<List<EventLocationVM>> GetLocationsForAdmin();

        Task<bool> EditEventOccurrenceAttendees(EventOccurrenceAtendeesVM model, long OccurrenceId);
        Task<List<GetEventOccuranceByUserDto>> GetEventOccuranceByUser(GetEventOccuranceByUserVM model);

        Task<bool> SendEventOcuurenceInvitation(long eventOcuurenceId);

    }
}
