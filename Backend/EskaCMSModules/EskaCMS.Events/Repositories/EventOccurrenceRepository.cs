using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;
using EskaCMS.Events.Models;
using EskaCMS.Events.Repositories.Interfaces;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Events;
using Microsoft.Extensions.DependencyInjection;
using EskaCommerce.Notifications.Notifiers;
using Newtonsoft.Json.Linq;

namespace EskaCMS.Events.Repositories
{
    public class EventOccurrenceRepository : IEventOccurrenceRepository
    {
        private readonly IRepository<EventOccurrence> _eventOccurrenceRepository;
        private readonly IRepository<EventAttendees> _eventAttendeesRepository;
        private readonly IWorkContext _workContext;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public EventOccurrenceRepository(IRepository<EventOccurrence> eventOccurrenceRepository,
            IServiceScopeFactory serviceScopeFactory,
            IRepository<EventAttendees> eventAttendeesRepository,
            IWorkContext workContext)
        {
            _eventAttendeesRepository = eventAttendeesRepository;
            _eventOccurrenceRepository = eventOccurrenceRepository;
            _workContext = workContext;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<EventOccurrenceVM> GetById(long id)
        {
            return await _eventOccurrenceRepository.Query().Where(x => x.Id == id).Select(x => new EventOccurrenceVM
            {
                Description = x.Description,
                ModifiedById = x.ModifiedById,
                ModificationDate = DateTimeOffset.Now,
                EventId = x.EventId,
                FromTime = x.FromTime,
                LocationId = x.LocationId,
                Status = x.Status,
                ThumbnailId = x.ThumbnailId,
                MaxEnrollment = x.MaxEnrollment,
                Name = x.Name,
                Settings = x.Settings,
                ReservationDate = x.ReservationDate,
                ToTime = x.ToTime,
                CreatedById = x.CreatedById

            }).FirstOrDefaultAsync();
        }

        public async Task<long> Create(EventOccurrenceVM OccurrenceVM)
        {
            var siteId = await _workContext.GetCurrentSiteIdAsync();
            var userId = await _workContext.GetCurrentUserId();

            var OccurrenceEntity = new EventOccurrence();
            OccurrenceEntity.CreatedById = userId;
            OccurrenceEntity.CreationDate = DateTimeOffset.Now;
            OccurrenceEntity.Description = OccurrenceVM.Description;
            OccurrenceEntity.EventId = OccurrenceVM.EventId;
            OccurrenceEntity.ReservationDate = OccurrenceVM.ReservationDate;
            OccurrenceEntity.ToTime = OccurrenceVM.ToTime;
            OccurrenceEntity.ThumbnailId = OccurrenceVM.ThumbnailId;
            OccurrenceEntity.Status = EStatus.Active;
            OccurrenceEntity.Settings = OccurrenceVM.Settings;
            OccurrenceEntity.LocationId = OccurrenceVM.LocationId;
            OccurrenceEntity.MaxEnrollment = OccurrenceVM.MaxEnrollment;
            OccurrenceEntity.FromTime = OccurrenceVM.FromTime;
            OccurrenceEntity.SiteId = siteId;

            _eventOccurrenceRepository.Add(OccurrenceEntity);
            await _eventOccurrenceRepository.SaveChangesAsync();

            if (OccurrenceVM.EventOccurrenceAtendees != null)
            {
                await AddEventOcurranceAtendees(OccurrenceVM.EventOccurrenceAtendees, OccurrenceEntity.Id);
            }


            await HandleEmailMeetings(OccurrenceEntity.Id);

            return OccurrenceEntity.Id;
        }



        public async Task<bool> HandleEmailMeetings(long eventOccurrenceId)
        {

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var _workContext = scope.ServiceProvider.GetRequiredService<IWorkContext>();
                var _testNotifier = scope.ServiceProvider.GetRequiredService<ITestNotifier>();
                var _eventOccurrenceRepo = scope.ServiceProvider.GetRequiredService<IRepository<EventOccurrence>>();

                var cultureId = _workContext.GetCurrentCultureId();

                var eventOccurrence = await _eventOccurrenceRepo.Query()
                                                          .Where(x => x.Id == eventOccurrenceId)
                                                          .Include(x => x.Location)
                                                          .ThenInclude(x => x.EventLocationDetails)
                                                          .Include(x => x.CreatedBy)
                                                          .Include(x => x.EventAttendees)
                                                          .ThenInclude(x => x.User)
                                                          .AsNoTracking()
                                                          .FirstOrDefaultAsync();

                if (eventOccurrence is null)
                    return false;

                var allUserEmails = eventOccurrence.EventAttendees.Where(x => x.UserId.HasValue)
                                                                  .Select(x => x.User.Email)
                                                                  .ToList()
                                                                  .Concat(eventOccurrence.EventAttendees.Where(x => !x.UserId.HasValue)
                                                                                                        .Select(x => x.Email)
                                                                                                        .ToList());

                var location = eventOccurrence.Location.EventLocationDetails.Where(x => x.CultureId == cultureId).FirstOrDefault()?.Name ?? "";
                await _testNotifier.SendMeetingEmail(eventOccurrence.CreatedBy.Email, eventOccurrence.CreatedBy.FullName, allUserEmails.ToList(), eventOccurrence.Description, eventOccurrence.Settings ?? "", location, eventOccurrence.FromTime, eventOccurrence.ToTime, eventOccurrence.SiteId);



                return true;
            }

        }


        private async Task<bool> AddEventOcurranceAtendees(EventOccurrenceAtendeesVM occurranceAtendeesVM, long eventoccuranceId)
        {

            var externalUsers = occurranceAtendeesVM.ExternalAtendees.Select(x => new EventAttendees
            {
                Email = x,
                EventOccurrenceId = eventoccuranceId,
            }).ToList();

            var InternalUsers = occurranceAtendeesVM.InternalAtendees.Select(x => new EventAttendees
            {
                UserId = x,
                EventOccurrenceId = eventoccuranceId,
            }).ToList();

            var AttendeesToBeAdded = externalUsers.Concat(InternalUsers);

            if (AttendeesToBeAdded.Any())
            {

                _eventAttendeesRepository.AddRange(AttendeesToBeAdded);
                await _eventAttendeesRepository.SaveChangesAsync();

            }

            return true;
        }

        public async Task Delete(long Id)
        {
            var reservation = await _eventOccurrenceRepository.Query().FirstOrDefaultAsync(x => x.Id == Id);
            reservation.Status = EStatus.Deleted /*Delete*/;
            _eventOccurrenceRepository.Update(reservation);
            await _eventOccurrenceRepository.SaveChangesAsync();
        }

        public async Task<bool> Update(EventOccurrenceVM OccurrenceVM, long OccurrenceId)
        {
            var EntityOccurance = await _eventOccurrenceRepository.Query().Where(x => x.Id == OccurrenceId).FirstOrDefaultAsync();
            EntityOccurance.Description = OccurrenceVM.Description;
            EntityOccurance.ModifiedById = OccurrenceVM.ModifiedById;
            EntityOccurance.ModificationDate = DateTimeOffset.Now;
            EntityOccurance.EventId = OccurrenceVM.EventId;
            EntityOccurance.ReservationDate = OccurrenceVM.ReservationDate;
            EntityOccurance.FromTime = OccurrenceVM.FromTime;
            EntityOccurance.LocationId = OccurrenceVM.LocationId;
            EntityOccurance.Status = OccurrenceVM.Status;
            EntityOccurance.ThumbnailId = OccurrenceVM.ThumbnailId;
            EntityOccurance.MaxEnrollment = OccurrenceVM.MaxEnrollment;
            EntityOccurance.Name = OccurrenceVM.Name;
            EntityOccurance.Settings = OccurrenceVM.Settings;
            EntityOccurance.ToTime = OccurrenceVM.ToTime;

            _eventOccurrenceRepository.Update(EntityOccurance);
            await _eventOccurrenceRepository.SaveChangesAsync();
            return true;
        }

        public async Task<EventOccurrenceVM> GetEventOccuranceByEventId(long EventId, EStatus? Status)
        {
            return await _eventOccurrenceRepository.Query().Where(x => x.EventId == EventId && ((x.Status == Status) || Status == null))
                                                            .Include(x => x.EventAttendees)
                                                            .ThenInclude(x => x.User)
                                                            .Select(x => new EventOccurrenceVM
                                                            {
                                                                Description = x.Description,
                                                                ModifiedById = x.ModifiedById,
                                                                ModificationDate = DateTimeOffset.Now,
                                                                EventId = x.EventId,
                                                                FromTime = x.FromTime,
                                                                LocationId = x.LocationId,
                                                                Status = x.Status,
                                                                ThumbnailId = x.ThumbnailId,
                                                                MaxEnrollment = x.MaxEnrollment,
                                                                Name = x.Name,
                                                                Settings = x.Settings,
                                                                ReservationDate = x.ReservationDate,
                                                                ToTime = x.ToTime,
                                                                EventOccurrenceAtendees = new EventOccurrenceAtendeesVM
                                                                {
                                                                    InternalUsers = x.EventAttendees.Where(x => x.UserId.HasValue)
                                                                                                    .Select(x => new InternalAttendeesDto { Email = x.User.Email, FullName = x.User.FullName, Id = x.User.Id })
                                                                                                    .ToList(),
                                                                    ExternalAtendees = x.EventAttendees.Where(x => !x.UserId.HasValue)
                                                                                                       .Select(x => x.Email)
                                                                                                       .ToList(),

                                                                }

                                                            }).FirstOrDefaultAsync();
        }

        public async Task<List<EventOccurrenceVM>> GetEventOccuranceByDate(DateTimeOffset ReservationDate, long? LocationId, EStatus? Status = null)
        {
            var siteId = await _workContext.GetCurrentSiteIdAsync();
            var cultureId = _workContext.GetCurrentCultureId();
            return await _eventOccurrenceRepository.Query().Where(x => x.SiteId == siteId &&
            (x.FromTime.Date.Equals(ReservationDate.Date) || x.ToTime.Date.Equals(ReservationDate.Date)) && (x.LocationId == LocationId || LocationId == null)
             && (x.Status != EStatus.Deleted && ((x.Status == Status) || (Status == null))))
                .Include(x => x.CreatedBy)
                .ThenInclude(x => x.CustomerGroups)
                .ThenInclude(x => x.CustomerGroup)
                .ThenInclude(x => x.customerGroupDetails)
                .Include(x => x.EventAttendees)
                .ThenInclude(x => x.User)
                .ThenInclude(x => x.CustomerGroups)
                .ThenInclude(x => x.CustomerGroup)
                .ThenInclude(x => x.customerGroupDetails)
                .Select(x => new EventOccurrenceVM
                {
                    Id = x.Id,
                    Description = x.Description,
                    ModifiedById = x.ModifiedById,
                    ModificationDate = DateTimeOffset.Now,
                    EventId = x.EventId,
                    FromTime = x.FromTime,
                    LocationId = x.LocationId,
                    Status = x.Status,
                    ThumbnailId = x.ThumbnailId,
                    MaxEnrollment = x.MaxEnrollment,
                    Name = x.Name ?? x.Description,
                    Settings = x.Settings,
                    ReservationDate = x.ReservationDate,
                    ToTime = x.ToTime,
                    CreatedById = x.CreatedById,
                    CoreUsername = x.CreatedBy.UserName,
                    FullName = x.CreatedBy.FullName,
                    EventOccurrenceAtendees = new EventOccurrenceAtendeesVM
                    {
                        ExternalAtendees = x.EventAttendees.Where(x => !x.UserId.HasValue)
                                                        .Select(x => x.Email)
                                                        .ToList(),

                        InternalUsers = x.EventAttendees.Where(x => x.UserId.HasValue)
                                                        .Select(x => new InternalAttendeesDto
                                                        {
                                                            Email = x.User.Email,
                                                            FullName = x.User.FullName,
                                                            Id = x.User.Id,
                                                            DepartmentId = !string.IsNullOrEmpty(x.User.CustomerGroups.Select(x => x.CustomerGroup.AdditionalInfo).FirstOrDefault())
                                                                ? JObject.Parse(x.User.CustomerGroups.Select(x => x.CustomerGroup.AdditionalInfo).FirstOrDefault())["DepartmentId"].ToString() ?? null
                                                                : null,
                                                            DepartmentName = !x.User.CustomerGroups.Any() ? null : x.User.CustomerGroups.SelectMany(x => x.CustomerGroup.customerGroupDetails).Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                                                        })
                                                        .ToList(),


                    },
                    DepartmentId = !string.IsNullOrEmpty(x.CreatedBy.CustomerGroups.Select(x => x.CustomerGroup.AdditionalInfo).FirstOrDefault())
                                                ? JObject.Parse(x.CreatedBy.CustomerGroups.Select(x => x.CustomerGroup.AdditionalInfo).FirstOrDefault())["DepartmentId"].ToString() ?? null
                                                : null,
                    DepartmentName = !x.CreatedBy.CustomerGroups.Any() ? null : x.CreatedBy.CustomerGroups.SelectMany(x => x.CustomerGroup.customerGroupDetails).Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                    Email = x.CreatedBy.Email
                }).ToListAsync();
        }

        private string? GetDepartmentIdFromJson(string? json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                var parsed = JObject.Parse(json);
                return parsed["DepartmentId"]?.ToString();
            }
            catch
            {
                return null;
            }
        }
        public async Task<List<GetEventOccuranceByUserDto>> GetEventOccuranceByUser(GetEventOccuranceByUserVM model)
        {
            var siteId = await _workContext.GetCurrentSiteIdAsync();

            var userId = model.UserId.HasValue ? model.UserId.Value : await _workContext.GetCurrentUserId();

            var result = _eventOccurrenceRepository.Query()
                                                    .Where(x => x.SiteId == siteId &&
                                                     (model.From.HasValue ? x.ReservationDate.Date >= model.From.Value.Date : true) && (model.To.HasValue ? x.ReservationDate.Date <= model.To.Value.Date : true))
                                                    .Where(x => x.Status != EStatus.Deleted)
                                                    .Include(x => x.CreatedBy)
                                                    .Include(x => x.EventAttendees)
                                                    .ThenInclude(x => x.User)
                                                    .Where(x => x.EventAttendees.Any(x => x.UserId == userId) || x.CreatedById == userId)
                                                    .AsNoTracking().ToList()
                                                    .GroupBy(x => x.ReservationDate.Date)
                                                    .Select(x => new
                                                    {
                                                        ReservationDate = x.Key,
                                                        Items = x.ToList()

                                                    }


                                                    ).ToList();

            var data = result.Select(x => new GetEventOccuranceByUserDto
            {
                ReservationDate = x.ReservationDate,
                Events = x.Items.Select(y => new GetEventOccuranceByUserListDto
                {
                    CoreUsername = y.CreatedBy.UserName,
                    Description = y.Description,
                    FromTime = y.FromTime,
                    ToTime = y.ToTime,
                    Id = y.Id,
                    Name = y.Name,
                    EventOccurrenceAtendees = new EventOccurrenceAtendeesVM
                    {
                        ExternalAtendees = y.EventAttendees.Where(x => !x.UserId.HasValue).Select(y => y.Email).ToList(),
                        InternalUsers = y.EventAttendees.Where(x => x.UserId.HasValue)
                                                        .Select(x => new InternalAttendeesDto { Email = x.User.Email, FullName = x.User.FullName, Id = x.User.Id })
                                                        .ToList(),

                    }
                }).ToList()



            }).ToList();


            return data;

        }



        public async Task<bool> EditEventOccurrenceAtendees(long occurrenceId, EventOccurrenceAtendeesVM model)
        {
            var siteId = await _workContext.GetCurrentSiteIdAsync();

            var attendees = await _eventAttendeesRepository.Query()
                                                           .Where(x => x.EventOccurrenceId == occurrenceId && x.EventOccurrence.SiteId == siteId)
                                                           .ToListAsync();


            _eventAttendeesRepository.RemoveRange(attendees);
            await _eventAttendeesRepository.SaveChangesAsync();


            await AddEventOcurranceAtendees(model, occurrenceId);

            return true;





        }

        public async Task<List<EventOccurrenceVM>> GetEventOccuranceByLocationId(long LocationId, EStatus? Status = null)
        {
            return await _eventOccurrenceRepository.Query().Include(x => x.CreatedBy)
                .Where(x => x.LocationId == LocationId && ((x.Status == Status) || Status == null)).Select(x => new EventOccurrenceVM
                {
                    Description = x.Description,
                    ModifiedById = x.ModifiedById,
                    ModificationDate = DateTimeOffset.Now,
                    EventId = x.EventId,
                    ReservationDate = x.ReservationDate,
                    FromTime = x.FromTime,
                    LocationId = x.LocationId,
                    Status = x.Status,
                    ThumbnailId = x.ThumbnailId,
                    MaxEnrollment = x.MaxEnrollment,
                    Name = x.Name,
                    Settings = x.Settings,
                    ToTime = x.ToTime,
                    CreatedById = x.CreatedById,
                    CoreUsername = x.CreatedBy.UserName
                }).ToListAsync();
        }
    }
}
