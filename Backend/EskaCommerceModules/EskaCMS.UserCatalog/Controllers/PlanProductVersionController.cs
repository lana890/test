using EskaCommerce.UserCatalog.Dtos;
using EskaCommerce.UserCatalog.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using EskaCMS.Infrastructure.Web.SmartTable;
using Microsoft.AspNetCore.Authorization;

namespace EskaCMS.PlanProductVersions.Controllers
{
    [Route("api/PlanProductVersion/[action]")]
    [ApiController]
    [Authorize]

    public class PlanProductVersionController : ControllerBase
    {
        private readonly IPlanProductVersionService _planServices;
        public PlanProductVersionController(IPlanProductVersionService planServices)
        {
            _planServices = planServices;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PlanProductVersionCreate category)
        {
            try
            {
                await _planServices.Create(category);
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
                return Ok(_planServices.Get(id));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPatch]
        public async Task<IActionResult> Update([FromForm] PlanProductVersionUpdate category)
        {
            try
            {
                await _planServices.Update(category);
                return Ok("Plan Product Version has been updated successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> GetAll([FromBody] SmartTableParam input)
        {
            try
            {
                return Ok(await _planServices.GetAll(input));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public IActionResult GetProductsByPlan(int PlanId)
        {
            try
            {
                return Ok(_planServices.GetProductsByPlan(PlanId));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        

    }
}
