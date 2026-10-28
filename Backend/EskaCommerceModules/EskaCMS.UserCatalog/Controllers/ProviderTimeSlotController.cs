using EskaCommerce.UserCatalog.Dtos;
using EskaCommerce.UserCatalog.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace EskaCMS.TimeSlots.Controllers
{
    [Route("api/ProviderTimeSlot/[action]")]
    [ApiController]
    [Authorize]

    public class ProviderTimeSlotController : ControllerBase
    {
        private readonly IProviderTimeSlotsService _timeSlotServices;
        public ProviderTimeSlotController(IProviderTimeSlotsService timeSlotServices)
        {
            _timeSlotServices = timeSlotServices;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProviderTimeSlotCreateVM timeSlot)
        {
            try
            {
                return Ok(await _timeSlotServices.Create(timeSlot));
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
                return Ok(_timeSlotServices.Get(id));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetProviderTimeSlots(long providerId)
        {
            try
            {
                return Ok(await _timeSlotServices.GetProviderTimeSlots(providerId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPatch]
        public async Task<IActionResult> Update([FromBody] ProviderTimeSlotUpdateVM timeSlot)
        {
            try
            {
                await _timeSlotServices.Update(timeSlot);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
