using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace EskaCMS.Plans.Controllers
{
    [Route("api/BackgroundSettings/[action]")]
    [ApiController]
    [Authorize]

    public class BackgroundSettingsController : ControllerBase
    {
        private readonly IBackgroundSettingsService _backgroundSettingsService;
        public BackgroundSettingsController(IBackgroundSettingsService backgroundSettingsService)
        {
            _backgroundSettingsService = backgroundSettingsService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BackgroundServiceSettingsCreate request)
        {
            try
            {

                string response = await _backgroundSettingsService.Create(request);
                return Ok(response);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPatch]
        public async Task<IActionResult> Update([FromBody] BackgroundServiceSettingsUpdate request)
        {
            try
            {
                return Ok(await _backgroundSettingsService.Update(request));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> GetList([FromBody] ListRequestVM<BackgroundServiceSettingsListRequest> request)
        {
            try
            {
                return Ok(await _backgroundSettingsService.GetList(request));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get(string id)
        {
            try
            {
                return Ok(await _backgroundSettingsService.Get(id));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                return Ok(await _backgroundSettingsService.GetAll());

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
