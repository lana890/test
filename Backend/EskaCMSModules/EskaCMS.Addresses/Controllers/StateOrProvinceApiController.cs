using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Addresses.ViewModels;
using EskaCMS.Addresses.Services.Interfaces;
using System;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Core.Extensions;
using EskaCMS.Addresses.Model;

namespace EskaCMS.Addresses.Controllers
{

    [Route("api/states-provinces")]
    [Authorize]
    public class StateOrProvinceApiController : Controller
    {
        private readonly IRepository<StateOrProvince> _stateOrProvinceRepository;
        private readonly IStateOrProvinceApiService _StateOrProvinceApiService;
        private readonly IWorkContext workContext;
        private readonly IOutputCacheStore _outputCacheStore;

        public StateOrProvinceApiController(IStateOrProvinceApiService StateOrProvinceApiService, IWorkContext workContext, IRepository<StateOrProvince> stateOrProvinceRepository, /* IRepositoryWithTypedId<Country, string> countryRepository, */ IOutputCacheStore outputCacheStore)
        {
            _stateOrProvinceRepository = stateOrProvinceRepository;
            _StateOrProvinceApiService = StateOrProvinceApiService;
            this.workContext = workContext;
            _outputCacheStore = outputCacheStore;
        }
        [HttpPost("grid")]
        public IActionResult List(string countryId, [FromBody] StateSmartSearchTableParam<StateSearchListVM> param)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(_StateOrProvinceApiService.GetStatesByCountryId(param, countryId, SiteId));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }

        [HttpGet("states")]
        [OutputCache(PolicyName = "reference")]
        public async Task<IActionResult> GetStatesByCountryId(string countryId)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _StateOrProvinceApiService.GetStatesByCountryId(countryId, SiteId));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            try
            {
                return Ok(await _StateOrProvinceApiService.GetStateById(id));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }

        [HttpPut("{id}")]
        //[Authorize(Roles = "admin")]
        public async Task<IActionResult> Put(long id, [FromBody] StateOrProvinceResponseRequestForm model)
        {
            try
            {
                var result = await _StateOrProvinceApiService.EditState(id, model);
                await _outputCacheStore.EvictByTagAsync("reference", default);
                return Ok(result);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        //[Authorize(Roles = "admin")]
        public async Task<IActionResult> Post([FromBody] StateOrProvinceResponseRequestForm model)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var result = await _StateOrProvinceApiService.CreateState(model, SiteId);
                return Ok(result);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        //[Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var result = await _StateOrProvinceApiService.DeleteState(id);
                return Ok(result);
            }
            catch (DbUpdateException)
            {
                return BadRequest(new { Error = $"This state can not be deleted because it is referenced to other tables" });
            }
        }


        [HttpGet("/api/countries/{countryId}/states-provinces")]
        [OutputCache(PolicyName = "reference")]
        public async Task<IActionResult> GetStatesOrProvinces(string countryId)
        {
            var cultureId = workContext.GetCurrentCultureId();
            var statesOrProvinces = await _stateOrProvinceRepository.Query().Include(x => x.StateOrProvinceDetails)
                .AsNoTracking()
                .Where(x => x.CountryId == countryId)
                .Select(x => new
                {
                    Id = x.Id,
                    Name = x.StateOrProvinceDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault()
                })
                .OrderBy(x => x.Name)
                .ToListAsync();

            return Ok(statesOrProvinces);
        }




        [HttpPost("ShippableStates")]
        public async Task<IActionResult> ShippableStates([FromBody] SearchShippableStatesVM model)
        {
            var result = await _StateOrProvinceApiService.GetShippableStates(model.countryId, model.pagesize,model.pagenumber, model.searchtext,model.stateId);

            return Ok(result);
        }
        
        [HttpGet("ShippableStates/{CountryId}")]
        public async Task<IActionResult> ShippableStates(string CountryId)
        {
            var result = await _StateOrProvinceApiService.GetShippableStates(CountryId, 1000000, 0, "",null);

            return Ok(result);
        }
    }
}
