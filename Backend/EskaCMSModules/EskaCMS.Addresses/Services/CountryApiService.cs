using EskaCMS.Addresses.Services.Interfaces;
using EskaCMS.Addresses.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Address;
using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Web.SmartTable;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EskaCMS.Addresses.Services
{
    public class CountryApiService : ICountryApiService
    {
        private readonly IWorkContext workContext;
        private readonly IRepositoryWithTypedId<Country, string> _CountryRepository;
        private readonly IRepository<StateOrProvince> _StateOrProvinceRepository;
        private readonly IRepository<CountryDetails> countryDetailsRepository;
        private readonly IMemoryCache _memoryCache;
        private const string AllCountriesCacheKeyPrefix = "addresses:countries:all";
        private static readonly ConcurrentDictionary<string, byte> CountryCacheKeys = new ConcurrentDictionary<string, byte>();
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> CountryCacheLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

        public CountryApiService(
            IWorkContext workContext,
            IRepositoryWithTypedId<Country, string> CountryRepository,
            IRepository<StateOrProvince> StateOrProvinceRepository,
            IRepository<CountryDetails> CountryDetailsRepository,
            IMemoryCache memoryCache
            )
        {
            this.workContext = workContext;
            _CountryRepository = CountryRepository;
            _StateOrProvinceRepository = StateOrProvinceRepository;
            countryDetailsRepository = CountryDetailsRepository;
            _memoryCache = memoryCache;
        }


        public async Task<IEnumerable> GetAllCountries()
        {
            try
            {
                var cultureId = workContext.GetCurrentCultureId();
                var cacheKey = $"{AllCountriesCacheKeyPrefix}:{cultureId}";
                if (_memoryCache.TryGetValue(cacheKey, out List<CountryForm> cachedCountries))
                {
                    return cachedCountries;
                }

                var cacheLock = CountryCacheLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
                await cacheLock.WaitAsync();

                try
                {
                    if (_memoryCache.TryGetValue(cacheKey, out cachedCountries))
                    {
                        return cachedCountries;
                    }

                    var Countries = await _CountryRepository.Query().AsNoTracking().Include(x => x.CountryDetails).Select(x => new CountryForm
                    {
                        Id = x.Id,
                        Code3 = x.Code3,
                        Name = x.CountryDetails.Where(x => cultureId == 0 || cultureId == x.CultureId).Select(x => x.Name).FirstOrDefault()

                    }).ToListAsync();

                    var result = Countries.OrderBy(C => C.Name)
                                          .Where(x => x.Code3 != "ISR")
                                          .ToList();

                    _memoryCache.Set(cacheKey, result, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                        SlidingExpiration = TimeSpan.FromMinutes(10)
                    });
                    CountryCacheKeys.TryAdd(cacheKey, 0);

                    return result;
                }
                finally
                {
                    cacheLock.Release();
                }
            }
            catch (Exception exc)
            {

                throw;
            }
        }
        public CountrySmartGridOutputVM<CountryForm> GetCountries(CountrySmartSearchTableParam<CountrySearchListVM> param, long SiteId)
        {
            var cultureId = workContext.GetCurrentCultureId();

            CountrySearchListVM search = param.Search;
            var CountryQuery = _CountryRepository.Query().AsNoTracking()
                                       .Where(x => x.Status == Entities.Enum.GeneralEnums.EStatus.Published)
                                      .Include(x => x.CountryDetails)
                                      .Where(x =>
                                          (string.IsNullOrEmpty(search.Name) ||
                                          (x.CountryDetails.Any(i => i.Name.ToUpper().Contains(search.Name.ToUpper())))))
                                       .ToList();

            CountrySmartGridOutputVM<CountryForm> countries = new CountrySmartGridOutputVM<CountryForm>
            {
                Items = CountryQuery.Select(c => new CountryForm
                {
                    Id = c.Id,
                    Name = c.CountryDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                    Code3 = c.Code3,
                    IsShippingEnabled = c.IsShippingEnabled,
                    IsBillingEnabled = c.IsBillingEnabled,
                    IsCityEnabled = c.IsCityEnabled,
                    IsZipCodeEnabled = c.IsZipCodeEnabled,
                    IsDistrictEnabled = c.IsDistrictEnabled,
                    IsRegional = c.IsRegional,

                }).Skip(param.Pagination.Number * param.Pagination.Start).Take(param.Pagination.Number).OrderBy(x => x.Name).ToList(),
                TotalRecord = CountryQuery.Count()
            };

            return countries;


        }
        public async Task<CountryRequestForm> GetCountryById(string id)
        {
            var country = await _CountryRepository.Query().Include(x => x.CountryDetails).Where(x => x.Id == id).Select(country => new CountryRequestForm
            {

                Id = country.Id,
                countryDetails = country.CountryDetails.Select(x => new CountryDetailsForm
                {

                    CultureId = x.CultureId,
                    Name = x.Name,


                }).ToList(),
                Code3 = country.Code3,
                IsBillingEnabled = country.IsBillingEnabled,
                IsShippingEnabled = country.IsShippingEnabled,
                IsCityEnabled = country.IsCityEnabled,
                IsZipCodeEnabled = country.IsZipCodeEnabled,
                IsDistrictEnabled = country.IsDistrictEnabled,
                IsWebCountryView = country.IsWebCountryView,
                IsRegional = country.IsRegional
            }).FirstOrDefaultAsync();


            return country;
        }

        //public async Task<CountryForm> GetCountryViewAvailable()
        //{
        //    var CountryQuery = _CountryRepository.Query()
        //                                    .Select(c => new CountryForm
        //                                    {
        //                                        Id = c.Id,
        //                                        Name = c.Name,
        //                                        Code3 = c.Code3,
        //                                        IsShippingEnabled = c.IsShippingEnabled,
        //                                        IsBillingEnabled = c.IsBillingEnabled,
        //                                        IsCityEnabled = c.IsCityEnabled,
        //                                        IsZipCodeEnabled = c.IsZipCodeEnabled,
        //                                        IsDistrictEnabled = c.IsDistrictEnabled
        //                                    })
        //                               .ToList();




        //}
        public async Task<bool> EditCountry(string Id, CountryRequestForm model)
        {
            try
            {


                var country = await _CountryRepository.Query().FirstOrDefaultAsync(x => x.Id == Id);

                country.Code3 = model.Code3;
                country.IsShippingEnabled = model.IsShippingEnabled;
                country.IsBillingEnabled = model.IsBillingEnabled;
                country.IsCityEnabled = model.IsCityEnabled;
                country.IsZipCodeEnabled = model.IsZipCodeEnabled;
                country.IsDistrictEnabled = model.IsDistrictEnabled;
                country.IsWebCountryView = model.IsWebCountryView;
                country.IsRegional = model.IsRegional;

                await _CountryRepository.SaveChangesAsync();

                var CountryDetails = await countryDetailsRepository.Query().Where(x => x.CountryId == Id).ToListAsync();

                foreach (var CountryDetail in CountryDetails)
                {
                    foreach (var modelcountrydetails in model.countryDetails)
                    {
                        if (CountryDetail.CultureId == modelcountrydetails.CultureId)
                        {

                            CountryDetail.Name = modelcountrydetails.Name;
                        }

                    }
                }
                await countryDetailsRepository.SaveChangesAsync();

                ClearCountryCaches();

                return true;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<bool> CreateCountry(CountryRequestForm model, long SiteId)
        {
            try
            {


                var country = new Country(model.Id)
                {
                    Code3 = model.Code3,
                    IsBillingEnabled = model.IsBillingEnabled,
                    IsShippingEnabled = model.IsShippingEnabled,
                    IsCityEnabled = model.IsCityEnabled,
                    IsZipCodeEnabled = model.IsZipCodeEnabled,
                    IsDistrictEnabled = model.IsDistrictEnabled,
                    IsWebCountryView = model.IsWebCountryView,
                    IsRegional = model.IsRegional,
                    Status = Entities.Enum.GeneralEnums.EStatus.Published
                };
                _CountryRepository.Add(country);
                await _CountryRepository.SaveChangesAsync();
                var CountryDetails = new List<CountryDetails>();

                if (model.countryDetails.Count() > 0)
                {
                    CountryDetails = model.countryDetails.Select(x => new CountryDetails
                    {
                        CountryId = country.Id,
                        Name = x.Name,
                        CultureId = x.CultureId,


                    }).ToList();
                    countryDetailsRepository.AddRange(CountryDetails);
                    await countryDetailsRepository.SaveChangesAsync();
                }

                ClearCountryCaches();

                return true;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<bool> DeleteCountry(string Id)
        {
            try
            {

                var country = await _CountryRepository.Query().Include(x => x.StatesOrProvinces).FirstOrDefaultAsync(x => x.Id == Id);

                country.Status = Entities.Enum.GeneralEnums.EStatus.Deleted;
                await _CountryRepository.SaveChangesAsync();

                ClearCountryCaches();

                return true;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<CountryReponseDto>> ShippingEnabled(bool IsShippingEnabled = false)
        {
            var cultureId = workContext.GetCurrentCultureId();

            var query = _CountryRepository.Query().Include(x => x.CountryDetails).Where(x => x.Status == Entities.Enum.GeneralEnums.EStatus.Published)
                                                                                 .Where(x => x.IsShippingEnabled == IsShippingEnabled);

            var countries = await query
                .Select(x => new CountryReponseDto
                {
                    Id = x.Id,
                    Name = x.CountryDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault()
                }).OrderBy(x => x.Name).ToListAsync();
            return countries;
        }


        public async Task<List<CountryReponseDto>> GetUsedCountries()
        {
            var cultureId = workContext.GetCurrentCultureId();

            var query = _CountryRepository.Query().Where(x => x.Status == Entities.Enum.GeneralEnums.EStatus.Published).Include(x => x.CountryDetails);

            var countries = await query
                .Select(x => new CountryReponseDto
                {
                    Id = x.Id,
                    Name = x.CountryDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault()
                }).OrderBy(x => x.Name).ToListAsync();
            return countries;

        }
        //States

        public StateSmartGridOutputVM<StateOrProvinceForm> GetStatesByCountryId(StateSmartSearchTableParam<StateSearchListVM> param, string countryId, long siteId)
        {
            StateSearchListVM search = param.Search;
            var cultureId = workContext.GetCurrentCultureId();
            var stateQuery = _StateOrProvinceRepository.Query().Include(x => x.StateOrProvinceDetails)
                                                                                                  .Where(x => x.Status == Entities.Enum.GeneralEnums.EStatus.Published)
                                                                                                  .Include(x => x.Country)
                                                                                                  .Where(x => x.Status == Entities.Enum.GeneralEnums.EStatus.Published)
                                                                                                  .Where(x => x.CountryId == countryId);

            if (!string.IsNullOrEmpty(search.Name))
            {

                stateQuery = stateQuery.Where(x => x.StateOrProvinceDetails.Any(x => x.Name.Contains(search.Name)));
            }
            StateSmartGridOutputVM<StateOrProvinceForm> states = new StateSmartGridOutputVM<StateOrProvinceForm>
            {
                Items = stateQuery.Select(s => new StateOrProvinceForm
                {
                    Id = s.Id,
                    Name = s.StateOrProvinceDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                    Code = s.Code,
                    CountryCode = s.Country.Id
                }).Skip(param.Pagination.Number * param.Pagination.Start).Take(param.Pagination.Number).OrderBy(x => x.Name).ToList(),
                TotalRecord = stateQuery.Count()
            };

            return states;
        }


        public async Task<List<CountryForm>> GetEnabledCountry(long siteId)
        {
            try
            {
                var cultureId = workContext.GetCurrentCultureId();

                var country = await _CountryRepository.Query().Include(x => x.CountryDetails).Where(x => x.Status == Entities.Enum.GeneralEnums.EStatus.Published).Where(x => x.IsWebCountryView).Select(country => new CountryForm
                {

                    Id = country.Id,
                    Name = country.CountryDetails.Where(x => x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                    Code3 = country.Code3,
                    IsBillingEnabled = country.IsBillingEnabled,
                    IsShippingEnabled = country.IsShippingEnabled,
                    IsCityEnabled = country.IsCityEnabled,
                    IsZipCodeEnabled = country.IsZipCodeEnabled,
                    IsDistrictEnabled = country.IsDistrictEnabled,
                    IsWebCountryView = country.IsWebCountryView,
                    IsRegional = country.IsRegional,
                }).ToListAsync();


                return country;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private void ClearCountryCaches()
        {
            foreach (var cacheKey in CountryCacheKeys.Keys)
            {
                _memoryCache.Remove(cacheKey);
            }

            CountryCacheKeys.Clear();
        }
    }
}
