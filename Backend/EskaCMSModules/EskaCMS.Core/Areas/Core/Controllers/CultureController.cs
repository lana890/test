using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Areas.Core.Controllers
{
    [ApiController]
    [Route("api/Culture")]
    [Authorize]
    public class CultureController : ControllerBase
    {
        private readonly ICultureService _cultureService;
        public CultureController(ICultureService cultureService)
        {
            _cultureService = cultureService;
        }

        [HttpPost]
        [Route("GetAllCultures")]

        public async Task<IActionResult> GetAllCultures()
        {
            try
            {
                return Ok(await _cultureService.GetCultures());
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }

        [HttpGet]
        [Route("GetById/{Id}")]

        public async Task<IActionResult> GetById([FromRoute] string Id)
        {
            try
            {
                return Ok(await _cultureService.GetById(Id));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }
        [HttpDelete]
        [Route("RemoveCulture/{Id}")]

        public async Task<IActionResult> RemoveCulture([FromRoute] string Id)
        {
            try
            {
                return Ok(await _cultureService.RemoveCulture(Id));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
        [HttpPatch]
        [Route("UpdateCulture")]

        public async Task<IActionResult> UpdateCulture([FromBody] CultureViewModel cultureViewModel )
        {
            try
            {
                return Ok( await _cultureService.UpdateCulture(cultureViewModel));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }
        [HttpPost]
        [Route("AddCulture")]

        public async Task<IActionResult> AddCulture([FromBody] CultureViewModel cultureViewModel)
        {
            try
            {
                return Ok(await _cultureService.AddCulture(cultureViewModel));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }

    } 
}
