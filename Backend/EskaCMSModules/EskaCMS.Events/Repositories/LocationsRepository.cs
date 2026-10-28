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
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Events;

using EskaCMS.Entities.Entity;

namespace EskaCMS.Events.Repositories
{
    public class LocationsRepository : ILocationsRepository
    {
        private readonly IRepository<EventLocation> _eventLocationRepository;
        private readonly IRepository<EventLocationDetails> _eventLocationDetailsRepository;
        private readonly IWorkContext _workContext;
        public LocationsRepository(
            IRepository<EventLocation> eventLocationRepository,
            IRepository<EventLocationDetails> eventLocationDetailsRepository,
            IWorkContext workContext)
        {
            _eventLocationRepository = eventLocationRepository;
            _workContext = workContext;
            _eventLocationDetailsRepository = eventLocationDetailsRepository;
        }

        public async Task<EventLocationVM> GetById(long id)
        {


            return await _eventLocationRepository.Query().Where(x => x.Id == id)
                                                        .Include(x => x.EventLocationDetails)
                                                        .Select(x => new EventLocationVM
                                                        {

                                                            LocationMapURL = x.LocationMapURL,
                                                            ThumbnailId = x.ThumbnailId,
                                                            Status = x.Status,
                                                            CreationDate = x.CreationDate,
                                                            Settings = x.Settings,
                                                            SiteId = x.SiteId,
                                                            EventPlace=x.EventPlace,
                                                            CategoryId = x.CategoryId,
                                                            EventLocationDetails = x.EventLocationDetails.Select(y => new EventLocationDetailsVM
                                                            {
                                                                CultureId = y.CultureId,
                                                                CultureCode = y.Culture.CultureId,
                                                                Name = y.Name
                                                            }).ToList(),

                                                        }).FirstOrDefaultAsync();
        }

        public async Task<long> Create(EventLocationVM Location)
        {
            var SiteId = await _workContext.GetCurrentSiteIdAsync();
            var LocationEntity = new EventLocation();
            LocationEntity.CreatedById = await _workContext.GetCurrentUserId();
            LocationEntity.CreationDate = DateTimeOffset.Now;
            LocationEntity.LocationMapURL = Location.LocationMapURL;
            LocationEntity.Settings = Location.Settings;
            LocationEntity.SiteId = SiteId;
            LocationEntity.Status = EStatus.Active;
            LocationEntity.CategoryId = Location.CategoryId.HasValue && Location.CategoryId != 0 ? Location.CategoryId : null;
            LocationEntity.ThumbnailId = Location.ThumbnailId;
            LocationEntity.EventPlace = Location.EventPlace;
            LocationEntity.EventLocationDetails = Location.EventLocationDetails.Any() ? Location.EventLocationDetails.Select(x => new EventLocationDetails
            {

                Name = x.Name,
                CultureId = x.CultureId
            })
                .ToList() : new List<EventLocationDetails>();
            _eventLocationRepository.Add(LocationEntity);
            await _eventLocationRepository.SaveChangesAsync();

            return LocationEntity.Id;
        }


        //public async Task<bool> AddEventLocationDetails(List<EventLocationDetailsVM> eventLocationDetails, long eventLocationId)
        //{
        //    var eventLocationDetailsToBeAdded = eventLocationDetails.Select(x => new EventLocationDetails
        //    {
        //        EventLocationId = eventLocationId,
        //        Name = x.Name,
        //        CultureId = x.CultureId
        //    })
        //        .ToList();

        //    if (eventLocationDetailsToBeAdded.Any())
        //    {
        //        _eventLocationDetailsRepository.AddRange(eventLocationDetailsToBeAdded);
        //        await _eventLocationDetailsRepository.SaveChangesAsync();
        //    }

        //    return true;
        //}




        public async Task Delete(long id)
        {
            var location = await _eventLocationRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
            location.Status = EStatus.Deleted /*Delete*/;
            _eventLocationRepository.Update(location);
            await _eventLocationRepository.SaveChangesAsync();
        }

        public async Task<bool> Update(EventLocationVM LocationVM, long LocationId)
        {
            var SiteId = await _workContext.GetCurrentSiteIdAsync();
            var UserId = await _workContext.GetCurrentUserId();
            EventLocation EntityLocation = new EventLocation();

            EntityLocation = await _eventLocationRepository.Query()
                                                           .Where(x => x.Id == LocationId)
                                                           .Include(x=>x.EventLocationDetails)
                                                           .FirstOrDefaultAsync();

            EntityLocation.ModifiedById = UserId;
            EntityLocation.ModificationDate = DateTimeOffset.Now;
            EntityLocation.LocationMapURL = LocationVM.LocationMapURL;

            EntityLocation.Settings = LocationVM.Settings;
            EntityLocation.SiteId = SiteId;
            EntityLocation.Status = LocationVM.Status != 0 ? LocationVM.Status : EntityLocation.Status;
            EntityLocation.ThumbnailId = LocationVM.ThumbnailId != 0 ? LocationVM.ThumbnailId : null;
            EntityLocation.Longitude = LocationVM.Longitude;
            EntityLocation.Latitude = LocationVM.Latitude;
            EntityLocation.CategoryId = LocationVM.CategoryId;
            EntityLocation.EventPlace = LocationVM.EventPlace;
            await UpdateEventLocationDetails(EntityLocation.EventLocationDetails, LocationVM.EventLocationDetails, LocationId);
            return true;
        }



        public async Task<bool> UpdateEventLocationDetails(List<EventLocationDetails> savedEventLocationDetails, List<EventLocationDetailsVM> eventLocationDetails, long eventLocationId)
        {


            foreach (var item in eventLocationDetails)
            {
                var locationDetails = savedEventLocationDetails.Where(x => x.CultureId == item.CultureId).FirstOrDefault();
                if (locationDetails != null)
                {
                    locationDetails.Name = item.Name;


                    _eventLocationDetailsRepository.Update(locationDetails);
                }
                else
                {
                    var neweventLocationDetails = new EventLocationDetails
                    {
                        Name = item.Name,
                        CultureId = item.CultureId,

                        EventLocationId = eventLocationId

                    };

                    _eventLocationDetailsRepository.Add(neweventLocationDetails);
                }
            }
            await _eventLocationDetailsRepository.SaveChangesAsync();

            await _eventLocationRepository.SaveChangesAsync();
            return true;

        }



        public async Task<List<EventLocationVM>> GetLocationsBySiteId()
        {

            var cultureId = _workContext.GetCurrentCultureId();

            var SiteId = await _workContext.GetCurrentSiteIdAsync();
            return await _eventLocationRepository.Query().Include(x => x.Category)
                .Include(x => x.EventLocationDetails)
                .Where(x => x.SiteId == SiteId &&
                x.Status == EStatus.Active && 
            x.EventLocationDetails.Any(y => y.CultureId == cultureId))
                .Select(x => new EventLocationVM
                {
                    Id = x.Id,
                    Name = x.EventLocationDetails.Where(e => e.CultureId == cultureId).FirstOrDefault().Name,
                    LocationMapURL = x.LocationMapURL,
                    ThumbnailId = x.ThumbnailId,
                    Status = x.Status,
                    CreationDate = x.CreationDate,
                    Settings = x.Settings,
                    SiteId = x.SiteId,
                    EventPlace = x.EventPlace,
                    CategoryId = x.CategoryId,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude
                }).ToListAsync();
        }


        public async Task<List<EventLocationVM>> GetLocationsForAdmins()
        {

            var SiteId = await _workContext.GetCurrentSiteIdAsync();
            return await _eventLocationRepository.Query().Include(x => x.Category)
                .Include(x => x.EventLocationDetails)
                .Where(x => x.SiteId == SiteId &&
                x.Status != EStatus.Deleted)
                .Select(x => new EventLocationVM
                {
                    Id = x.Id,

                    LocationMapURL = x.LocationMapURL,
                    ThumbnailId = x.ThumbnailId,
                    Status = x.Status,
                    CreationDate = x.CreationDate,
                    Settings = x.Settings,
                    SiteId = x.SiteId,
                    EventPlace = x.EventPlace,
                    CategoryId = x.CategoryId,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    EventLocationDetails = x.EventLocationDetails.Select(e => new EventLocationDetailsVM
                    {
                        CultureId = e.CultureId,
                        CultureCode = e.Culture.CultureId,
                        Name = e.Name
                    })
                     .ToList()
                }).ToListAsync();
        }

        public async Task<bool> ToggleStatus(long LocationId, EStatus LocationStatus)
        {
            var userId = await _workContext.GetCurrentUserId();
            var EntityLocationEvent = await _eventLocationRepository.Query().Where(x => x.Id == LocationId).FirstOrDefaultAsync();
            EntityLocationEvent.ModifiedById = userId;
            EntityLocationEvent.ModificationDate = DateTimeOffset.Now;
            EntityLocationEvent.Status = LocationStatus == EStatus.Active ? EStatus.Inactive : EStatus.Active;
            _eventLocationRepository.Update(EntityLocationEvent);
            await _eventLocationRepository.SaveChangesAsync();
            return true;
        }




    }
}
