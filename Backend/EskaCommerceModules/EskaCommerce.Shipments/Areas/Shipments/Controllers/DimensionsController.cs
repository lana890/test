using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Helpers;
using EskaCommerce.Shipments.Models;
using EskaCommerce.Shipments.Services.Shippment.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Shipments.Areas.Shipments.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DimensionsController : ControllerBase
    {
        private readonly IDimensionService _service;

        public DimensionsController(IDimensionService service)
        {
            _service = service;
        }

        [HttpPost("GetAll")]
        public async Task<IActionResult> GetAll([FromBody] DimensionFilterationViewModel model)
        {
            try
            {
                var result = await _service.GetAllAsync(model);
                return Ok(result);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
         
        }

        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
                if (result == null)
                    return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        
        }
        [HttpGet("GetDimensionsType")]
        public IActionResult GetDimensionsType([FromRoute] int id)
        {
            try
            {
                var model = EnumHelper.ToDictionary(typeof(EDimensionType)).Select(x => new { Id = x.Key, Name = x.Value });
                return Ok(model);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] DimensionCreateUpdateViewModel model)
        {
            try
            {
                var result = await _service.CreateAsync(model);
                if (result == null)
                    return BadRequest("Creation failed");
                return Ok();
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
           
        }

        [HttpPut("Update")]
        public async Task<IActionResult> Update( [FromBody] DimensionCreateUpdateViewModel model)
        {
            try
            {
                var result = await _service.UpdateAsync( model);
                if (result == null)
                    return NotFound();
                return Ok();
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
         
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete([FromRoute]int id)
        {
            try
            {
                var deleted = await _service.DeleteAsync(id);
                return Ok();
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
          
        }

    }

}
