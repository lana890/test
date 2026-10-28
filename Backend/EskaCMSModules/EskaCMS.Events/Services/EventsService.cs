using EskaCMS.Core.Data;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Events.Models;
using EskaCMS.Events.Repositories;
using EskaCMS.Events.Repositories.Interfaces;
using EskaCMS.Events.Services.Interfaces;
using EskaCMS.Events.Services.Repositories;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static EskaCMS.Entities.Enum.GeneralEnums;
using static EskaCMS.Entities.Enum.EventEnums;
using EskaCMS.DataAccess.Data;

namespace EskaCMS.Events.Services
{
    public class EventsService : IEventsService
    {
        private readonly IEventsRepository _eventsRepositroy;
        private readonly ILocationsRepository _locationsRepository;
        private readonly EskaDCMSDbContext _context;
        private readonly IEventOccurrenceRepository _eventOccurrenceRepository;
        private readonly IWorkContext _workContext;

        public EventsService(IEventsRepository eventsRepositroy,
            ILocationsRepository locationsRepository,
            EskaDCMSDbContext context,
            IEventOccurrenceRepository eventOccurrenceRepository,
            IWorkContext workContext
            )
        {
            _eventsRepositroy = eventsRepositroy;
            _locationsRepository = locationsRepository;
            _context = context;
            _eventOccurrenceRepository = eventOccurrenceRepository;
            _workContext = workContext;
        }

        #region Categories
        public async Task<List<CategoryViewModel>> GetCategoriesByTypeId()
        {
            try
            {
                return await _eventsRepositroy.GetCategoriesByTypeId();
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<bool> ChangeCategoryName(long catId, CategoryViewModel categoryVM)
        {
            try
            {
                return await _eventsRepositroy.UpdateCategory(catId, categoryVM);
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<long> AddCategory(long catId, CategoryViewModel category)
        {
            try
            {
                if (catId == 0)
                {
                    return await _eventsRepositroy.AddCategory(category);
                }
                else
                {
                    await _eventsRepositroy.UpdateCategory(catId, category);
                }


                return 0;

            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<bool> DeleteCategory(long catId)
        {
            try
            {
                await _eventsRepositroy.DeleteCategory(catId);
                return true;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        #endregion

        #region Events
        public async Task<List<EventVM>> GetAllEvents(string Name, string Description, DateTimeOffset? FromDate, DateTimeOffset? ToDate, DateTimeOffset? FromTime, DateTimeOffset? ToTime, long? LocationId, long? CultureId, long? CategoryId, EStatus? Status,long? ParentId)
        {
           return await _eventsRepositroy.GetAll(Name, Description, FromDate, ToDate, FromTime, ToTime, LocationId, CultureId, CategoryId, Status, ParentId);
        }

        public async Task<EventVM> GetEventById(long EventId)
        {
            return await _eventsRepositroy.GetById(EventId);
        }

        public async Task<List<EventVM>> GetEventByParentId(long EventId, long CultureId)
        {
            return await _eventsRepositroy.GetByParentId(EventId);
        }

        public async Task<List<EventVM>> GetEventByCategoryId(long CatId)
        {
            return await _eventsRepositroy.GetEventsByCategoryId(CatId);
        }

        public async Task<long> AddEvent(List<EventVM> eventVMList)
        {
            // since events list 
            if (eventVMList.Count > 0)
            {
                if (ValidAddEvent(eventVMList[0]))
                {
                    var strategy = _context.Database.CreateExecutionStrategy();
                    return await strategy.ExecuteAsync(async () =>
                    {
                        await using var transaction = await _context.Database.BeginTransactionAsync();
                        try
                        {
                            long id = 0;
                            for (int x = 0; x < eventVMList.Count; x++)
                            {
                                var eventVM = eventVMList[x];
                                eventVM.ParentId = id != 0 ? id : null;
                                var tempId = await _eventsRepositroy.Create(eventVM);
                                if (x == 0)
                                {
                                    id = tempId;
                                    eventVM.ParentId = id;
                                    await _eventsRepositroy.Update(eventVM,id);
                                    if (eventVM.EventPlace == EventPlace.InHouse)
                                    {
                                        for (int i = 0; i < eventVM.NumberOfDays; i++)
                                        {
                                            EventOccurrenceVM eventOccurrenceVM = new EventOccurrenceVM()
                                            {
                                                EventId = id,
                                                LocationId = eventVM.LocationId.Value,
                                                FromTime = eventVM.FromTime.AddDays(i),
                                                ToTime = eventVM.ToTime.AddDays(i),
                                                MaxEnrollment = eventVM.MaxEnrollment,
                                                ReservationDate = eventVM.FromDate.AddDays(i),
                                                ThumbnailId = eventVM.ThumbnailId
                                            };
                                            var resultObj = await AddEventOccurrence(eventOccurrenceVM);
                                            if (!resultObj.IsValidEndDate || !resultObj.IsValidStartDate)
                                            {
                                                await transaction.RollbackAsync();
                                                throw new InvalidOperationException();
                                            }
                                        }
                                    }
                                }
                            }
                            await transaction.CommitAsync();
                            return id;
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            throw ExceptionHelper.ThrowDeepException(ex);
                        }
                    });
                }
                else
                {
                    throw new Exception("Cannot add this event, overlap detected");
                }
            }
            else
            {
                throw new Exception("No Events were sent to be added");
            }
        }

        public async Task<bool> EditEvent(List<EventVM> EventVMList, long EventId)
        {
            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        if (EventVMList.Count > 0)
                        {
                            await _eventsRepositroy.UpdateByParentId(EventVMList, EventId);
                            await transaction.CommitAsync();
                            return true;
                        }
                        else
                        {
                            throw new Exception("No events list to be updated");
                        }
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw new Exception();
                    }
                });
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<bool> ToggleStatus(long EventId, EStatus EventStatus)
        {
            try
            {
                return await _eventsRepositroy.ToggleStatus(EventId, EventStatus);
            }

            catch (Exception exc)
            {

                throw exc;
            }
        }



        /// <summary>
        /// Valid Event
        /// </summary>
        /// <param name="oEvent"></param>
        /// <param name="FromDate"></param>
        /// <param name="oDateTime"></param>
        /// <param name="EndDate"></param>
        /// <returns></returns>
        private static bool ValidAddEvent(EventVM oEvent)
        {
            var Date = oEvent.FromDate.ToString("MM/dd/yyyy");

            if (oEvent.FromDate < oEvent.RequestDate || oEvent.ToDate < oEvent.FromDate || oEvent.ToDate < oEvent.RequestDate)
                return false;
            //if (oEvent.ToDate == oDateTime && EndDateEndTime < EndDateStartTime)
            //    return false;
            if (oEvent.ToDate == oEvent.RequestDate && oEvent.RequestDate.Add(new TimeSpan(23, 59, 59)) < oEvent.ToDate)
                return false;

            switch (oEvent.RepeatType)
            {
                case RepeatType.OneTime://One Time
                    return true;

                case RepeatType.Periodic://Periodic
                    switch (oEvent.PeriodType)
                    {
                        case PeriodType.Days:
                            return (((oEvent.RequestDate - oEvent.FromDate).Days % oEvent.PeriodInterval) == 0);
                        case PeriodType.Weeks:
                            return (((oEvent.RequestDate - oEvent.FromDate).Days % (oEvent.PeriodInterval * 7)) == 0);
                        case PeriodType.Months:
                            return (((oEvent.RequestDate - oEvent.FromDate).Days % (oEvent.PeriodInterval * 30)) == 0);
                    }
                    break;

                case RepeatType.Weekly://Weekly
                    return (((oEvent.RequestDate - oEvent.FromDate).Days % (oEvent.PeriodInterval * 7)) < 7
                        && GetIndexOfDay(oEvent.RequestDate.DayOfWeek.ToString()) == (int)oEvent.PeriodType);

                case RepeatType.Monthly://For Month
                    if (oEvent.PeriodInterval == (int)FirstLast.First)//First
                        return (oEvent.RequestDate.Day <= 7
                            && GetIndexOfDay(oEvent.RequestDate.DayOfWeek.ToString()) == oEvent.PeriodInterval);
                    if (oEvent.PeriodInterval == (int)(FirstLast.Last))//Last
                        return (oEvent.RequestDate.Day > (DateTime.DaysInMonth(oEvent.RequestDate.Year, oEvent.RequestDate.Month) - 7)
                            && GetIndexOfDay(oEvent.RequestDate.DayOfWeek.ToString()) == (int)oEvent.PeriodType);
                    break;

                case RepeatType.InMonth://In Month
                    DateTime dateTime = new DateTime(oEvent.FromDate.Year, oEvent.FromDate.Month, oEvent.FromDate.Day);
                    DateTime tempDate = dateTime.AddMonths(oEvent.PeriodInterval);
                    return (((oEvent.RequestDate.Month - tempDate.Month) % oEvent.PeriodInterval) == 0
                        && (int)oEvent.PeriodType == oEvent.RequestDate.Day);

                case RepeatType.Annual://Annual
                    return (oEvent.FromDate.Month == oEvent.RequestDate.Month && oEvent.FromDate.Day == oEvent.RequestDate.Day);
            }
            return false;
        }
        #endregion

        #region Locations

        public async Task<long> AddLocation(EventLocationVM Location)
        {
            try
            {
                return await _locationsRepository.Create(Location);
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }
        public async Task<bool> EditLocation(EventLocationVM LocationVM, long LocationId)
        {
            try
            {
                return await _locationsRepository.Update(LocationVM, LocationId);
            }

            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<List<EventLocationVM>> GetLocationsBySiteId()
        {
            return await _locationsRepository.GetLocationsBySiteId();
        }


        public async Task<List<EventLocationVM>> GetLocationsForAdmin()
        {
            return await _locationsRepository.GetLocationsForAdmins();
        }

        public async Task<EventLocationVM> GetLocationsById(long LocationId)
        {
            return await _locationsRepository.GetById(LocationId);
        }

        public async Task<bool> ToggleLocationStatus(long LocationId, EStatus LocationStatus)
        {
            try
            {
                return await _locationsRepository.ToggleStatus(LocationId, LocationStatus);
            }

            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<bool> DeleteLocation(long Id)
        {
            var reservationInRoomId = await _eventOccurrenceRepository.GetEventOccuranceByLocationId(Id);
            if (reservationInRoomId.Count > 0)
                return false;
            await _locationsRepository.Delete(Id);
            return true;
        }

        #endregion

        #region Event Occurrence

        public async Task<ValidReservationVM> AddEventOccurrence(EventOccurrenceVM OccurrenceVM)
        {
            try
            {
                var result = new ValidReservationVM();

      
                var targetLocation = await GetLocationsById(OccurrenceVM.LocationId);

       
                if (targetLocation.EventPlace == EventPlace.InHouse)
                {
                    result = await validateDate(OccurrenceVM);

                  
                    if (result.IsValidStartDate && result.IsValidEndDate)
                    {
                        await _eventOccurrenceRepository.Create(OccurrenceVM);
                    }
                }
                else
                {
                    
                    await _eventOccurrenceRepository.Create(OccurrenceVM);
                    result.IsValidStartDate = true;
                    result.IsValidEndDate = true;
                }

                return result;
            }

            catch (Exception exc)
            {

                throw exc;
            }
        }

        private async Task<ValidReservationVM> validateDate(EventOccurrenceVM OccurrenceVM)
        {
            ValidReservationVM validReservationVM = new ValidReservationVM();
            var test = TimeZoneInfo.FindSystemTimeZoneById(TimeZone.CurrentTimeZone.StandardName);
            var test2 = TimeZoneInfo.ConvertTimeFromUtc(OccurrenceVM.ReservationDate.DateTime, test);
            var reservationList = await _eventOccurrenceRepository.GetEventOccuranceByDate(OccurrenceVM.ReservationDate, OccurrenceVM.LocationId);
            if (reservationList.Count == 0)
            {
                validReservationVM.IsValidStartDate = true;
                validReservationVM.IsValidEndDate = true;
                return validReservationVM;
            }

            var invalidDate = reservationList.FirstOrDefault(x => x.FromTime > OccurrenceVM.FromTime && x.ToTime < OccurrenceVM.ToTime);
            if (invalidDate == null)
            {
                validReservationVM.IsValidEndDate = true;
                validReservationVM.IsValidStartDate = true;
            }
            else
            {
                validReservationVM.IsValidEndDate = false;
                validReservationVM.IsValidStartDate = false;
                return validReservationVM;
            }

            invalidDate = reservationList.FirstOrDefault(x => (x.FromTime < OccurrenceVM.FromTime && x.ToTime > OccurrenceVM.FromTime)
            || x.FromTime == OccurrenceVM.FromTime);
            if (invalidDate == null)
            {
                validReservationVM.IsValidStartDate = true;
            }
            else
            {
                validReservationVM.IsValidStartDate = false;
            }

            invalidDate = reservationList.Where(x => (x.FromTime < OccurrenceVM.ToTime && x.ToTime > OccurrenceVM.ToTime) || x.ToTime == OccurrenceVM.ToTime).FirstOrDefault();
            if (invalidDate == null)
            {
                validReservationVM.IsValidEndDate = true;
            }
            else
            {
                validReservationVM.IsValidEndDate = false;
            }

            return validReservationVM;
        }
        public async Task<bool> EditEventOccurrence(EventOccurrenceVM OccurrenceVM, long OccurrenceId)
        {


            try
            {
                return await _eventOccurrenceRepository.Update(OccurrenceVM, OccurrenceId);
            }

            catch (Exception exc)
            {

                throw exc;
            }
        }



        public async Task<bool> EditEventOccurrenceAttendees(EventOccurrenceAtendeesVM model, long OccurrenceId)
        {


            try
            {
                return await _eventOccurrenceRepository.EditEventOccurrenceAtendees(OccurrenceId, model);
            }

            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task DeleteEventOccurrence(long ReservationId)
        {
            try
            {
                var userId = await _workContext.GetCurrentUserId();
                var reservation = await _eventOccurrenceRepository.GetById(ReservationId);
                if (reservation.CreatedById == userId)
                {
                    await _eventOccurrenceRepository.Delete(ReservationId);
                }
                else
                {
                    throw new Exception("You are not allowed to delete this reservation");
                }
            }

            catch (Exception exc)
            {

                throw exc;
            }
        }
        public async Task<EventOccurrenceVM> GetEventOccuranceById(long EventOccuranceId)
        {
            return await _eventOccurrenceRepository.GetById(EventOccuranceId);
        }
        public async Task<EventOccurrenceVM> GetEventOccuranceEventId(long EventId, EStatus? Status)
        {
            return await _eventOccurrenceRepository.GetEventOccuranceByEventId(EventId, Status);
        }

        public async Task<List<EventOccurrenceVM>> GetEventOccuranceByDate(DateTimeOffset ReservationDate, long? LocationId, EStatus? Status)
        {
            return await _eventOccurrenceRepository.GetEventOccuranceByDate(ReservationDate, LocationId, Status);
        }

        public async Task<List<GetEventOccuranceByUserDto>> GetEventOccuranceByUser(GetEventOccuranceByUserVM model)
        {
            return await _eventOccurrenceRepository.GetEventOccuranceByUser(model);
        }
        public async Task<List<EventOccurrenceVM>> GetEventOccuranceLocationId(long LocationId, EStatus? Status)
        {
            return await _eventOccurrenceRepository.GetEventOccuranceByLocationId(LocationId, Status);
        }


        //send Email
        public async Task<bool> SendEventOcuurenceInvitation(long eventOcuurenceId)
        {

            await _eventOccurrenceRepository.HandleEmailMeetings(eventOcuurenceId);

            return true;
               
        }

        #endregion
    }

}
