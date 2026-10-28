using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Security.Controllers
{
    [Route("api/")]
    [ApiController]
    [Authorize]
    public class PasswordPolicyController : ControllerBase
    {
        private readonly IPasswordPolicyService _passwordPolicyService;
        private readonly IWorkContext _workContext;
        public PasswordPolicyController(IPasswordPolicyService passwordPolicyService , IWorkContext workContext)
        {
            _passwordPolicyService = passwordPolicyService;
            _workContext = workContext;
        }

        [HttpPut]
        [Route("PasswordPolicy/UpdatePasswordPolicyStatusAsync/{Id}/{Status}")]
        public async Task<IActionResult> UpdatePasswordPolicyStatusAsync([FromRoute] long Id, [FromRoute]EStatus Status)
        {
            try
            {
                await _passwordPolicyService.UpdatePasswordPolicyStatusAsync(Id, Status);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }
        [HttpPost]
        [Route("PasswordPolicy/UpsertPasswordPolicyAsync")]
        public async Task<IActionResult> UpsertPasswordPolicyAsync([FromBody] PolicyPasswordVM Model)
        {
            try
            {
               
                return Ok(await _passwordPolicyService.UpsertPasswordPolicyAsync(Model));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }
        [HttpPost]
        [Route("PasswordPolicy/IsValidPassword")]
        public async Task<IActionResult> IsValidPasswordAsync(IsValidPasswordVM Model)
        {
            try
            {
                return Ok(await _passwordPolicyService.GetPasswordViolationsAsync(Model.Password, Model.Username));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }
        [HttpGet]
        [Route("PasswordPolicy/GetBySiteId/{siteId}")]
        [Route("PasswordPolicy/GetBySiteId")]
        public async Task<IActionResult> GetBySiteId(long? siteId)
        {
            try
            {
                siteId= siteId.HasValue ? siteId.Value : await _workContext.GetCurrentSiteIdAsync();
                return Ok(await _passwordPolicyService.GetBySiteId(siteId.Value));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }
        [HttpPost]
        [Route("PasswordPolicy/GetRestrictedPasswords")]
        public async Task<IActionResult> GetRestrictedPasswords([FromBody] SearchParamsRestrictedVm search)
        {
            try
            {
                return Ok(await _passwordPolicyService.GetRestrictedPasswords(search));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }
        [HttpPost]
        [Route("PasswordPolicy/UpsertRestrictedPasswords")]
        public async Task<IActionResult> UpsertRestrictedPasswords([FromBody]RestrictedPasswordsVM Model)
        {

            try
            {
                return Ok(await _passwordPolicyService.UpsertRestrictedPasswords(Model));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }
        [HttpPut]
        [Route("PasswordPolicy/UpdateRestrictedPasswordStatus/{Id}/{NewStatus}")]
        public  async Task<IActionResult> UpdateRestrictedPasswordStatus([FromRoute]long Id,[FromRoute] EStatus NewStatus)
        {
            try
            {
                return Ok(await _passwordPolicyService.UpdateRestrictedPasswordStatus(Id, NewStatus));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

    }
}
