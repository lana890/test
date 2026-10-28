using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using static BusinessModels.BusinessModel;
using EskaCMS.Core.Data;

using EskaCMS.Components.Models;
using EskaCMS.Components.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Web.SmartTable;

namespace EskaCMS.Components.Controllers

{
    [ApiController]
    [Route("api/PredefinedTemplates/[action]")]
    [Authorize]
    public class PredefinedTemplatesController : ControllerBase
    {
        private readonly IPredefinedTemplatesServices PredefinedTemplatesService;

        public PredefinedTemplatesController(IPredefinedTemplatesServices PredefinedTemplatesService)
        {
            this.PredefinedTemplatesService = PredefinedTemplatesService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSetting([FromBody] PredefinedTemplatesViewModel setting)
        {
            try
            {
                var result = await PredefinedTemplatesService.CreateSetting(setting);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSetting(int id)
        {
            try
            {
                await PredefinedTemplatesService.DeleteSetting(id);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetAllSettings([FromBody] PredefinedTemplatesSearchTableParam<SearchPredefinedTemplatesViewModel> model)
        {
            try
            {
                var result = await PredefinedTemplatesService.GetAllSettings(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PredefinedTemplatesViewModel>> GetSettingById(int id)
        {
            try
            {
                var setting = await PredefinedTemplatesService.GetSettingById(id);

                return Ok(setting);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpPut]
        public async Task<IActionResult> UpdateSetting( [FromBody] PredefinedTemplatesViewModel setting)
        {
            try
            {
                await PredefinedTemplatesService.UpdateSetting(setting.Id.Value, setting);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
