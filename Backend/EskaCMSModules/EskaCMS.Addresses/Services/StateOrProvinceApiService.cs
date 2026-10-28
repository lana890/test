using EskaCMS.Addresses.Model;
using EskaCMS.Addresses.Services.Interfaces;
using EskaCMS.Addresses.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Address;
using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Entities.Entity.ShippingTableRate;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Addresses.Services
{
    public class StateOrProvinceApiService : IStateOrProvinceApiService
    {
        private readonly IRepository<StateOrProvince> _StateOrProvinceRepository;
        private readonly IRepository<StateOrProvinceDetails> stateOrProvinceDetailsRepository;
        private readonly IRepository<PriceAndDestination> _priceAndDestinationRepository;
        private readonly IWorkContext _workContext;
        private readonly IUserSiteService _userSiteService;
        public StateOrProvinceApiService(
            IRepository<StateOrProvince> StateOrProvinceRepository,
            IRepository<StateOrProvinceDetails> StateOrProvinceDetailsRepository,
            IRepository<PriceAndDestination> priceAndDestinationRepository,
            IWorkContext workContext, IUserSiteService userSiteService)
        {
            _StateOrProvinceRepository = StateOrProvinceRepository;
            stateOrProvinceDetailsRepository = StateOrProvinceDetailsRepository;
            _workContext = workContext;
            _priceAndDestinationRepository = priceAndDestinationRepository;
            _userSiteService = userSiteService;
        }
        public StateSmartGridOutputVM<StateOrProvinceForm> GetStatesByCountryId(StateSmartSearchTableParam<StateSearchListVM> param, string countryId, long siteId)
        {
            var cultureId = _workContext.GetCurrentCultureId();

            StateSearchListVM search = param.Search;
            var stateQuery = _StateOrProvinceRepository.Query().AsNoTracking().Include(x => x.Country).Include(x => x.StateOrProvinceDetails).Where(x => x.Status == Entities.Enum.GeneralEnums.EStatus.Published)
                                      .Where(x => x.CountryId == countryId &&
                                          (string.IsNullOrEmpty(search.Name) ||
                                          x.StateOrProvinceDetails.Any(o => o.Name.ToUpper().Contains(search.Name.ToUpper()))))
                                       .ToList();
            StateSmartGridOutputVM<StateOrProvinceForm> states = new StateSmartGridOutputVM<StateOrProvinceForm>
            {
                Items = stateQuery.Select(s => new StateOrProvinceForm
                {
                    Id = s.Id,
                    Name = s.StateOrProvinceDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                    Code = s.Code,
                    CountryCode = s.Country.Id
                }).Skip(param.Pagination.Number * param.Pagination.Start).Take(param.Pagination.Number).OrderByDescending(x => x.Id).ToList(),
                TotalRecord = stateQuery.Count()
            };

            return states;
        }
        public async Task<List<StateOrProvinceForm>> GetStatesByCountryId(string countryId, long siteId)
        {
            var cultureId = _workContext.GetCurrentCultureId();

            var states = await _StateOrProvinceRepository.Query().Include(x => x.StateOrProvinceDetails).Where(x => x.CountryId == countryId).Where(x => x.Status == Entities.Enum.GeneralEnums.EStatus.Published)
                                     .Select(x => new StateOrProvinceForm
                                     {
                                         Id = x.Id,
                                         Name = x.StateOrProvinceDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault()
                                     })
                                      .ToListAsync();

            return states;
        }

        public async Task<StateOrProvinceResponseRequestForm> GetStateById(long id)
        {
            var state = await _StateOrProvinceRepository.Query().Include(x => x.StateOrProvinceDetails).Where(x => x.Id == id).Select(state => new StateOrProvinceResponseRequestForm
            {
                Id = state.Id,
                StateOrProvinceDetails = state.StateOrProvinceDetails.Select(x => new StateOrProvinceDetailsVm
                {
                    cultureId = x.CultureId,
                    Name = x.Name,

                }).ToList(),
                Code = state.Code,
                Type = state.Type,
                CountryCode = state.CountryId
            }).FirstOrDefaultAsync();


            return state;
        }
        public async Task<bool> EditState(long Id, StateOrProvinceResponseRequestForm model)
        {
            var state = await _StateOrProvinceRepository.Query().FirstOrDefaultAsync(x => x.Id == Id);
            state.Code = model.Code;
            state.Type = model.Type;

            await _StateOrProvinceRepository.SaveChangesAsync();
            var StateOrProvienceDetails = await stateOrProvinceDetailsRepository.Query().Where(x => x.StateOrProvinceId == Id).ToListAsync();

            foreach (var StateOrProvienceDetail in StateOrProvienceDetails)
            {
                foreach (var modelstatedetails in model.StateOrProvinceDetails)
                {
                    if (StateOrProvienceDetail.CultureId == modelstatedetails.cultureId)
                    {

                        StateOrProvienceDetail.Name = modelstatedetails.Name;
                    }

                }
            }
            await stateOrProvinceDetailsRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CreateState(StateOrProvinceResponseRequestForm model, long SiteId)
        {
            try
            {

                var state = new StateOrProvince
                {
                    Id = model.Id,
                    Code = model.Code,
                    CountryId = model.CountryId,
                    Type = model.Type,
                    Status = Entities.Enum.GeneralEnums.EStatus.Published

                };

                _StateOrProvinceRepository.Add(state);
                await _StateOrProvinceRepository.SaveChangesAsync();
                var StateAndProvinceDetails = new List<StateOrProvinceDetails>();

                if (model.StateOrProvinceDetails.Count() > 0)
                {
                    StateAndProvinceDetails = model.StateOrProvinceDetails.Select(x => new StateOrProvinceDetails
                    {
                        StateOrProvinceId = state.Id,
                        Name = x.Name,
                        CultureId = x.cultureId,


                    }).ToList();
                    stateOrProvinceDetailsRepository.AddRange(StateAndProvinceDetails);
                    await stateOrProvinceDetailsRepository.SaveChangesAsync();
                }
                return true;

            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<bool> DeleteState(long Id)
        {
            try
            {

                var state = await _StateOrProvinceRepository.Query().FirstOrDefaultAsync(x => x.Id == Id);

                state.Status = Entities.Enum.GeneralEnums.EStatus.Deleted;
                await _StateOrProvinceRepository.SaveChangesAsync();

                return true;


            }
            catch (Exception)
            {

                throw;
            }
        }


        public async Task<List<ShippableStatesVM>> GetShippableStates(string countryId, int? PageSize = 20,int ?PageNumber=0, string Search = null,long? StateId = null )
        {
            try
            {
                var SearchTextUpper = string.IsNullOrEmpty(Search) ? null : Search.ToUpper();
                var siteId = _workContext.GetCurrentSiteId();
                var siteCountry = await _userSiteService.GetSiteCountry(siteId);
                var cultureId = _workContext.GetCurrentCultureId();
                var pagenumber = PageNumber.HasValue ? PageNumber.Value : 0;
                var pagesize = PageSize ?? 10;
                var stateId = StateId!=null ||StateId!=0 ? StateId : null;

                var IsLocalCountry = siteCountry.ToUpper() == countryId.ToUpper();
                if (IsLocalCountry)
                {
                    var priceAndDestination = await _priceAndDestinationRepository.Query().AsNoTracking()
                                                                                  .Where(x => x.CountryId == countryId)
                                                                                  .Include(x => x.StateOrProvince)
                                                                                  .ThenInclude(x => x.StateOrProvinceDetails)
                                                                                   .Where(x => x.StateOrProvince.Status == Entities.Enum.GeneralEnums.EStatus.Published)
                                                                                  .ToListAsync();

                    var result = priceAndDestination.DistinctBy(x => x.StateOrProvinceId)
                                                    .Select(x => new ShippableStatesVM
                                                    {
                                                        Id = x.StateOrProvinceId.Value,
                                                        Name = x.StateOrProvince.StateOrProvinceDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault()
                                                    })
                                                    .Where(x => string.IsNullOrEmpty(SearchTextUpper) || x.Name.ToUpper().Contains(SearchTextUpper))
                                                    .OrderBy(x => x.Name)
                                                    .Skip(pagesize * (pagenumber))
                                                     .Take(pagesize)
                                                    .ToList();
                    return result;

                }

                var query = _StateOrProvinceRepository.Query()
                           .AsNoTracking()
                           .Where(x => x.Status == Entities.Enum.GeneralEnums.EStatus.Published)
                           .Where(x => x.CountryId == countryId)
                           .SelectMany(x => x.StateOrProvinceDetails)
                           .Where(x => cultureId == 0 || x.CultureId == cultureId);

                if (stateId.HasValue)
                {
                    query = query.Where(x => x.StateOrProvinceId == stateId.Value);
                }
                else if (!string.IsNullOrEmpty(SearchTextUpper))
                {
                    query = query.Where(x => x.Name.ToUpper().Contains(SearchTextUpper));
                }

                var statesOrProvinces = await query
                    .GroupBy(x => x.StateOrProvinceId)
                    .Select(g => new ShippableStatesVM
                    {
                        Id = g.Key,
                        Name = g.Select(x => x.Name).FirstOrDefault()
                    })
                    .OrderBy(x => x.Name)
                    .Skip(pagesize * pagenumber)
                    .Take(pagesize)
                    .ToListAsync();


                return statesOrProvinces;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

    }
}
