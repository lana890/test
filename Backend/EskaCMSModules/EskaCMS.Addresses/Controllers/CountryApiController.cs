using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Infrastructure.Data;
using System.Collections;
using System;
using EskaCMS.Addresses.Services.Interfaces;
using EskaCMS.Entities.Entity.Addresses;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;

namespace EskaCMS.Addresses.Controllers
{
    [Route("api/countries")]
    [Authorize]
    public class CountryApiController : Controller
    {
        private readonly ICountryApiService _CountryApiService;
        private readonly IOutputCacheStore _outputCacheStore;
        public CountryApiController(ICountryApiService CountryApiService, IOutputCacheStore outputCacheStore)
        {
             _CountryApiService = CountryApiService;
             _outputCacheStore = outputCacheStore;
        }


        [HttpGet("get-all-countries")]
        [AllowAnonymous]
        [OutputCache(PolicyName = "reference")]
        public async Task<IEnumerable> GetCountries()
        {
            try
            {
                var countries =await _CountryApiService.GetAllCountries();

                return countries;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] bool shippingEnabled = false)
        {
            try
            {
                return Ok(await _CountryApiService.ShippingEnabled(shippingEnabled));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }
        [HttpGet("GetFilterdCountries")]
        public async Task<IActionResult> GetFilterdCountries()
         {
            try
            {
                return Ok(await _CountryApiService.GetUsedCountries());
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }
        [HttpPost("grid")]
        public IActionResult List([FromBody] CountrySmartSearchTableParam<CountrySearchListVM> param)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(_CountryApiService.GetCountries(param, SiteId));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetEnabledCountry")]
        public async Task<IActionResult> GetEnabledCountry()
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _CountryApiService.GetEnabledCountry(SiteId));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            try
            {
                if (!string.IsNullOrEmpty(id))
                {
                    return Ok(await _CountryApiService.GetCountryById(id));
                }
                return BadRequest("Not found");
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        //[Authorize(Roles = "admin")]

        public async Task<IActionResult> Put(string id, [FromBody] CountryRequestForm model)
        {
            try
            {
                var result = await _CountryApiService.EditCountry(id, model);
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
        public async Task<IActionResult> Post([FromBody] CountryRequestForm model)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var result = await _CountryApiService.CreateCountry(model, SiteId);
                return Ok(result);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }

        [HttpDelete("{id}")]
        //[Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var result = await _CountryApiService.DeleteCountry(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = $"This country can not be deleted because it is referenced to other tables" });
            }


        }
    }
}
