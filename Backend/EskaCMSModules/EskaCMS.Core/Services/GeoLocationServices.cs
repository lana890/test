using EskaCMS.Core.Entities;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models.GeoLocation;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.UsersAndRolesManagement;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Services
{
    public class GeoLocationServices : IGeoLocationServices
    {
        private readonly IWorkContext workContext;
        private readonly IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository;
        private readonly IRepository<UserGeoLocation> userGeoLocation;

        public GeoLocationServices(IWorkContext workContext, IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository, IRepository<UserGeoLocation> userGeoLocation)
        {
            this.workContext = workContext;
            this.siteSettingsRepository = siteSettingsRepository;
            this.userGeoLocation = userGeoLocation;
        }

        public async Task AddGeoLocation()
        {
            var Latitude = workContext.GetCurrentGeoLocationLat();
            var Longitude = workContext.GetCurrentGeoLocationLon();
            var Address = workContext.GetCurrentGeoLocationAddress();
            var UserId =await workContext.GetCurrentUserId();
            if (!string.IsNullOrEmpty(Address))
            {

                string decodedAddress = Uri.UnescapeDataString(Address);

                var addressData = JsonConvert.DeserializeObject<dynamic>(decodedAddress);

                if (addressData != null)
                {
                    AddressGeoLocation UserInfo = await HandlerAddress(addressData);
                    await AddGeoLocation(addressData, decodedAddress, UserInfo, UserId, Latitude, Longitude);
                }


            }

        }

        private async Task<AddressGeoLocation> HandlerAddress(dynamic addressData)
        {
            try
            {
                var City = string.Empty;
                var State = string.Empty;
                var Country = string.Empty;

                 City = addressData?.state;
                if (string.IsNullOrEmpty(City))
                { 
                
                   City=addressData?.city;
                }
                 State = addressData?.state_district;
                if (string.IsNullOrEmpty(State))
                { 
                   State=addressData?.neighbourhood;
                
                }
                 Country = addressData?.country;
                return new AddressGeoLocation { 
                
                    City=City,
                    Area = State, 
                    Country=Country
                
                
                };

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task AddGeoLocation(dynamic addressData, string decodedFullSearch,AddressGeoLocation AddressGeoLocation, long userId, string Latitude,string Longitude)
        {
            try
            {
                var SiteId=workContext.GetCurrentSiteId();

                GeoLocationEnum geoLocationEnum = GeoLocationEnum.City;
                var GeoLocationSettingsEnum =await siteSettingsRepository.Query().Where(x => x.Id == "GeoLocation").Select(x=>x.Value).FirstOrDefaultAsync();

                if (GeoLocationSettingsEnum != null)
                {
                    geoLocationEnum = (GeoLocationEnum)Convert.ToInt32(GeoLocationSettingsEnum);
                }
                var UserGeoLocation =await userGeoLocation.Query().Where(x => x.UserId == userId).ToListAsync();

                if (UserGeoLocation.Count > 0)
                {

                    if (geoLocationEnum == GeoLocationEnum.City)
                    {
                        var City = AddressGeoLocation.City.ToLower();

                        if (UserGeoLocation.Exists(x => x.City == City))
                        {
                            return;
                        }

                    }
                    else if (geoLocationEnum == GeoLocationEnum.Area)
                    {
                        var Area = AddressGeoLocation.Area.ToLower();

                        if (UserGeoLocation.Exists(x => x.Area == Area))
                        {
                            return;
                        }
                    }
                    else
                    {
                        var Country = AddressGeoLocation.Country.ToLower();

                        if (UserGeoLocation.Exists(x => x.Country == Country))
                        {
                            return;
                        }
                    }

                }
                    var AddUserGeoLocation = new UserGeoLocation
                    {
                        City = AddressGeoLocation.City.ToLower(),
                        Country = AddressGeoLocation.Country.ToLower(),
                        Area = AddressGeoLocation.Area.ToLower(),
                        Latitude = Latitude,
                        Longitude = Longitude,
                        SiteId = SiteId,
                        UserId = userId,
                        FullAddress= decodedFullSearch,
                        CreationDate  = DateTime.Now,

                    };


                    userGeoLocation.Add(AddUserGeoLocation);
                    await userGeoLocation.SaveChangesAsync();

                


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<PaginationResult< UserAddressInfoResponse>> GetUserGeoLocation(UserGeoLocationSearchRequest model)
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();


                var query = await userGeoLocation.Query()
                                            .Include(x => x.User)
                                            .Where(x => x.SiteId == SiteId)
                                            .Where(x =>x.UserId == model.UserId)
                                            .AsNoTrackingWithIdentityResolution()
                                            .Select(y => new UserAddressInfoResponse
                                            {
                                             
                                                    UserId = y.UserId,
                                                    UserName = y.User.UserName,
                                                    Latitude = y.Latitude,
                                                    Longitude = y.Longitude,
                                                    Area = y.Area,
                                                    City = y.City,
                                                    Country = y.Country,
                                            }).PaginateAsync(model.PageSize, model.PageNumber);


                return query;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<List<GeoLocationAreaResponse>> GetUsersArea(GeoLocationSearchRequest model)
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();

                var Country = !string.IsNullOrEmpty(model.Country) ? model.Country.ToLower() : null;
                var City =!string.IsNullOrEmpty( model.City) ? model.City.ToLower() : null;

                var query = await userGeoLocation.Query()
                                            .Include(x => x.User)
                                            .AsNoTrackingWithIdentityResolution()
                                            .Where(x => x.SiteId == SiteId)
                                            .Where(x => Country == null || x.Country == Country)
                                            .Where(x => City == null || x.City == City)
                                            .Skip((model.PageNumber) * model.PageSize).Take(model.PageSize)
                                            .ToListAsync();

                 var Result = query.GroupBy(x => x.Area)
                                            .Select(x => new GeoLocationAreaResponse
                                            {
                                                Area = x.Key,
                                                usersAddressInfos = x.Select(y => new UsersAddressInfoResponse
                                                {
                                                    UserId = y.UserId,
                                                    UserName = y.User.UserName,
                                                    Latitude = y.Latitude,
                                                    Longitude = y.Longitude

                                                }).ToList()


                                            }).ToList();


                return Result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<List<GeoLocationCityResponse>> GetUsersCities(GeoLocationSearchRequest model)
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();

                var Country =string.IsNullOrEmpty( model.Country)?null:  model.Country.ToLower();

                var query = await userGeoLocation.Query()
                                            .Include(x => x.User)
                                            .Where(x => x.SiteId == SiteId && (Country == null || x.Country == Country))
                                            .AsNoTrackingWithIdentityResolution()
                                            .Skip((model.PageNumber) * model.PageSize).Take(model.PageSize)
                                            .ToListAsync();




                var Result= query.GroupBy(x => x.City)
                                            .Select(x => new GeoLocationCityResponse
                                            {
                                                City = x.Key,
                                                usersAddressInfos = x.Select(y => new UsersAddressInfoResponse
                                                {
                                                    UserId = y.UserId,
                                                    UserName = y.User.UserName,
                                                    Latitude = y.Latitude,
                                                    Longitude = y.Longitude
                                                }).ToList()


                                            }).ToList();


                return Result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<List<GeoLocationCountryResponse>> GetUsersCountries(int PageSize, int PageNumber)
        {
            try
            {
                var SiteId=workContext.GetCurrentSiteId();

                var query = await userGeoLocation.Query()
                                            .Include(x => x.User)
                                            .Where(x => x.SiteId == SiteId)
                                            .AsNoTrackingWithIdentityResolution()
                                            .Skip((PageNumber) * PageSize).Take(PageSize)
                                            .ToListAsync();


                var Result = query.GroupBy(x => x.Country)
                                                          .Select(x => new GeoLocationCountryResponse
                                                          {
                                                              Country = x.Key,
                                                              usersAddressInfos = x.Select(y => new UsersAddressInfoResponse
                                                              {
                                                                  UserId = y.UserId,
                                                                  UserName = y.User.UserName,
                                                                  Latitude = y.Latitude,
                                                                  Longitude = y.Longitude
                                                              }).ToList()


                                                          }).ToList();


                return Result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
}
