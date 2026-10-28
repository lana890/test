using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using System;
using EskaCMS.Addresses.Services.Interfaces;
using EskaCMS.Addresses.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Core.Extensions;

namespace EskaCMS.Core.Areas.Core.Controllers
{
    [Area("Core")]
    [Route("api/districts")]
    [Authorize]
    public class DistrictApiController : Controller
    {
        private readonly IRepository<District> _districtRepository;
        private readonly IWorkContext workContext;
        private readonly IDistrictApiService _DistrictApiService;
        private readonly IOutputCacheStore _outputCacheStore;
        public DistrictApiController(IRepository<District> districtRepository,IWorkContext workContext, IDistrictApiService DistrictApiService, IOutputCacheStore outputCacheStore)
        {
            _districtRepository = districtRepository;
            this.workContext = workContext;
            _DistrictApiService = DistrictApiService;
            _outputCacheStore = outputCacheStore;
        }

        [HttpPost("grid")]
        public IActionResult List(long stateId, [FromBody] DistrictSmartSearchTableParam<DistrictSearchListVM> param)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(_DistrictApiService.GetDistrictsByStateId(param, stateId, SiteId));

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
                return Ok(await _DistrictApiService.GetDistrictById(id));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }

        [HttpPut("{id}")]
        //[Authorize(Roles = "admin")]
        public async Task<IActionResult> Put(long id, [FromBody] DistrictRequestResponseVM model)
        {
            try
            {
                var result = await _DistrictApiService.EditDistrict(id, model);
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
        public async Task<IActionResult> Post([FromBody] DistrictRequestResponseVM model)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var result = await _DistrictApiService.CreateDistrict(model, SiteId);
                await _outputCacheStore.EvictByTagAsync("reference", default);
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
                var result = await _DistrictApiService.DeleteDistrict(id);
                await _outputCacheStore.EvictByTagAsync("reference", default);
                return Ok(result);
            }
            catch (DbUpdateException)
            {
                return BadRequest(new { Error = $"This district can not be deleted because it is referenced to other tables" });
            }
        }


        [HttpGet("states-provinces/{stateOrProvinceId}/districts")]
        [OutputCache(PolicyName = "reference")]
        public async Task<IActionResult> GetDistricts(long stateOrProvinceId)
        {
            var cultureId = workContext.GetCurrentCultureId();
            var districts = await _districtRepository
                .Query()
                .Include(x=>x.DistrictDetails)
                .Where(x => x.StateOrProvinceId == stateOrProvinceId)
                .Select(x => new
                {
                    x.Id,
                    Name = x.DistrictDetails.Where(x=> cultureId==0|| cultureId==x.CultureId).Select(x=>x.Name).FirstOrDefault()
                }).ToListAsync();

            return Json(districts);
        }
    }
}
 