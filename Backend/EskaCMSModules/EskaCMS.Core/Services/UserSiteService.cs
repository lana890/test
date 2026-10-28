using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Services
{
    public class UserSiteService : IUserSiteService
    {
        private readonly IRepository<UsersSites> _userSitesRepository;
        private readonly IRepositoryWithTypedId<Country, string> _countryRepository;
        private readonly IRepositoryWithTypedId<SiteSettings, string> _appSettingRepository;
        private readonly IRepository<EskaCMS.Entities.Entity.Core.Site> siteRepository;
        private readonly IWorkContext _workContext;
        

        public UserSiteService(IRepository<UsersSites> userSitesRepository,
                                IRepositoryWithTypedId<Country, string> countryRepository,
                               IRepositoryWithTypedId<SiteSettings, string> appSettingRepository,
                               IRepository<EskaCMS.Entities.Entity.Core.Site> SiteRepository,
                                IWorkContext workContext)
        {
            _workContext = workContext;
            _userSitesRepository = userSitesRepository;
            _countryRepository = countryRepository;
            _appSettingRepository = appSettingRepository;
            siteRepository = SiteRepository;
        }



        public async Task<UserCountryVM> GetUserCountry()
        {
            var userId = await _workContext.GetCurrentUserId();
            var siteId = await _workContext.GetCurrentSiteIdAsync();
            var cultureId =  _workContext.GetCurrentCultureId();

            var user = await _userSitesRepository
                                .Query()
                                .AsNoTracking()
                                .Where(u => u.SiteId == siteId && u.UserId == userId)
                                .Include(x => x.Country)
                                .ThenInclude(x=>x.CountryDetails)
                                .FirstOrDefaultAsync();

            var site= await siteRepository.Query()
                .AsNoTracking()
                .Where(x => x.Id == siteId)
                .Include(x => x.Country)
                .ThenInclude(x=>x.CountryDetails)
                .FirstOrDefaultAsync();

            var result = new UserCountryVM();
            if (user!=null&&user.Country != null)
            {
                result.Id = user.Country.Id;

                result.Name = user.Country.CountryDetails.Where(x=> cultureId == 0 || x.CultureId==cultureId).OrderByDescending(x=>x.Id).Select(x=>x.Name).FirstOrDefault();
                result.IsRegional = user.Country.IsRegional;

                return result;
            }

            else if (site.Country != null)
            {
                result.Id = site.Country.Id;
                result.Name = site.Country.CountryDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).OrderByDescending(x => x.Id).Select(x => x.Name).FirstOrDefault();

                result.IsRegional = site.Country.IsRegional;

                return result;

            }
          
            else
            {
                var siteSettings = _appSettingRepository.Query()
                    .AsNoTracking()
                                                    .Where(x => x.Id == "DefaultCountry")
                                                    .FirstOrDefault();

                if (siteSettings == null)
                    return new UserCountryVM();

                var countryId = siteSettings.Value;

                var country = _countryRepository.Query()
                                                .Where(x => x.Id == countryId)
                                                .Include(x => x.CountryDetails)
                                                .AsNoTracking()
                                                .FirstOrDefault();
                result.Name = country.CountryDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).OrderByDescending(x => x.Id).Select(x => x.Name).FirstOrDefault();

                result.Id = country.Id;
                result.IsRegional = country.IsRegional;

                return result;
            }



        }

        public async Task<bool> IsLocalCountry()
        {
            var userId = await _workContext.GetCurrentUserId();
            var siteId = await _workContext.GetCurrentSiteIdAsync();
            var userCountry =await GetUserCountryId(userId, siteId);
            var siteCountryId =await GetSiteCountryId(siteId);

            if (userCountry != null && siteCountryId != null)
            {
               

                return userCountry.IsRegional ? userCountry.IsRegional : userCountry.Id == siteCountryId;
            }
  
            else if (siteCountryId == null)
            {
                var siteSettings = await GetSiteSetting("DefaultCountry");
               
                if (siteSettings != null && userCountry!=null)
                {
                    return userCountry.IsRegional ? userCountry.IsRegional : userCountry.Id == siteSettings.Value;

                }
            }

            return true;
        }
        public async Task<Country> GetUserCountryId(long userId, long siteId)
        {
            return await _userSitesRepository
                .Query()
                .Where(u => u.SiteId == siteId && u.UserId == userId)
                .Select(x => x.Country)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<string> GetSiteCountry( long SiteId)
        { 
          var CountryId =await siteRepository.Query()
                .AsNoTracking()
                .Where(x => x.Id == SiteId)
                .Select(x => x.CountryId)
                .FirstOrDefaultAsync();

            if (CountryId == null)
            {
                var siteSettings = await GetSiteSetting("DefaultCountry");
                CountryId = siteSettings.Value;
            }

            return CountryId;

        }

      
        public async Task<List<long>> GetUserRoles(long siteId , long userId)
        {
            var result = await _userSitesRepository.Query()
                                              .Where(x => x.SiteId == siteId && x.UserId == userId)
                                              .SelectMany(x => x.User.UserRoles)
                                              .Select(x => x.RoleId)
                                              .ToListAsync();

            return result;
        }

        public async Task<string> GetSiteCountryId(long siteId)
        {
            return await siteRepository.Query()
                .Where(x => x.Id == siteId)
                .Select(x => x.CountryId)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task UpdateUserAvailablity(long userId, long siteId,ConnectionStatus connectionStatus)
        {
            try
            {
                var UserSite = await _userSitesRepository.Query().Where(x => x.SiteId == siteId && x.UserId == userId)
                                                                 .FirstOrDefaultAsync();
                if (UserSite == null)
                {

                    return;
                }
                if (connectionStatus == ConnectionStatus.Available)
                {
                    UserSite.UserConnectionStatus = connectionStatus;
                    UserSite.LastSeen = null;

                }
                else
                {
                    UserSite.UserConnectionStatus = connectionStatus;
                    UserSite.LastSeen = DateTimeOffset.Now;
                }
               await _userSitesRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private async Task<SiteSettings> GetSiteSetting(string settingId)
        {
            return await _appSettingRepository.Query()
                .Where(x => x.Id == settingId)
                 .AsNoTracking()
                .FirstOrDefaultAsync();
        }
    }
}
