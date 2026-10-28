using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.UserCatalog.Dtos;
using EskaCommerce.UserCatalog.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using EskaCommerce.UserCatalog.Dtos.CustomePlan;

namespace EskaCMS.Plans.Controllers
{
    [Route("api/Plan/[action]")]
    [ApiController]
 //   [Authorize]

    public class PlanController : ControllerBase
    {
        private readonly IPlanService _planServices;
        // private readonly IWebHostEnvironment _env;
        public PlanController(IPlanService planServices/* , IWebHostEnvironment env */)
        {
            _planServices = planServices;
            // _env = env;
        }


        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var siteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _planServices.Get(id,siteId));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        } 
        [HttpGet]
        public async Task<IActionResult> GetPlanWithProducts(long id)
        {
            try
            {
                var siteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _planServices.GetPlanWithProducts(id,siteId));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPlansList()
        {
            try
            {
                var siteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _planServices.GetPlansList(siteId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetPlanById(long id)
        {
            try
            {
                return Ok(await _planServices.GetPlanById(id));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> GetPlansByIds([FromBody]long[] ids)
        {
            try
            {
                var siteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _planServices.GetPlansByIds(ids,siteId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOneTimePlans()
        {
            try
            {
                var siteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _planServices.GetOneTimePlans(siteId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetRecurringPlans()
        {
            try
            {
                var siteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _planServices.GetRecurringPlans(siteId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> GetAllPlans([FromBody] SmartTableParam param)
        {
            try
            {
                return Ok(await _planServices.GetAll(param,false));
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        [HttpPost]
        public async Task<IActionResult> GetAllCustomePlan([FromBody] SmartTableParam param)
        {
            try
            {
                return Ok(await _planServices.GetAll(param, true));
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        [HttpPost]
        public async Task<IActionResult> CreateCustomePlan([FromBody] CustomePlanDto model)
        {
            try
            {
               ;
                return Ok(await _planServices.CreateCustomePlan(model));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        
        }

    }
}
