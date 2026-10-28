using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels.AppSettingsModels;
using EskaCMS.Core.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.NewsLetter.Controllers
{
    [ApiController]
    [Route("api/EmailSettings")]
    [Authorize]
    public class EmailSettingsController : ControllerBase
    {
        private readonly IEmailSettingsServices _EmailSettingsServices;
        public EmailSettingsController(IEmailSettingsServices emailSettingsServices)
        {
            _EmailSettingsServices = emailSettingsServices;
        }

        [HttpPost]
        [Route("UpsertEmailSettings")]
        public async Task<IActionResult> UpsertEmailSettings([FromBody] UpsertEmailSettingsVM Model)
        {
            try
            {
                await _EmailSettingsServices.UpsertEmailsSettigs(Model);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpGet]
        [Route("GetById/{id}")]

        public async Task<IActionResult> GetById(long Id)
        {
            try
            {
                return Ok(await _EmailSettingsServices.GetById(Id));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }

        [HttpGet]
        [Route("GetAllEmails")]
        public async Task<IActionResult> GetAllEmails()
        {
            try
            {
                return Ok(await _EmailSettingsServices.GetAllEmails());
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }

        [HttpPatch]
        [Route("UpdateStatus/{id}/{newStatus}")]
        public async Task<IActionResult> UpdateStatus([FromRoute] long Id, [FromRoute] EStatus NewStatus)
        {
            try
            {
                return Ok(await _EmailSettingsServices.UpdateEmailSettingsStatus(Id, NewStatus));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }





    }
}
