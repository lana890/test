using EskaCommerce.UserCatalog.Dtos;
using EskaCommerce.UserCatalog.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Areas.UserCatalog.Controllers
{
    [Route("api/PlansVersions/[action]")]
    [ApiController]
    [Authorize]

    public class PlansVersionsController : ControllerBase
    {
        private readonly IPlansVersionsService _planVersionsServices;
        public PlansVersionsController(IPlansVersionsService planVersionsServices)
        {
            _planVersionsServices = planVersionsServices;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PlansVersionsCreate plansVersionsInput)
        {
            try
            {
                var siteId = Convert.ToInt64(Request.Headers["SiteId"]);
                await _planVersionsServices.Create(plansVersionsInput, siteId);
                return Ok();

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public IActionResult Get(int id)
        {
            try
            {
                return Ok(_planVersionsServices.Get(id));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute]long id)
        {
            try
            {
                return Ok(await _planVersionsServices.Delete(id));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                return Ok(await _planVersionsServices.PlanVersionDetails(id));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetPlanVersions(long planId)
        {
            try
            {
                return Ok(await _planVersionsServices.GetPlanVersions(planId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


    }
}
