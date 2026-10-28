using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Controllers
{
    [Route("api/TwoFactorAuthentication/[action]")]
    [ApiController]
    [Authorize]

    public class TwoFactorAuthenticationController : ControllerBase
    {
        private readonly ITwoFactorAuthenticationServices twoFactorAuthentication;

        public TwoFactorAuthenticationController(ITwoFactorAuthenticationServices twoFactorAuthentication)
        {
            this.twoFactorAuthentication = twoFactorAuthentication;
        }

        [HttpPost]
        public async Task<IActionResult> VerifyTwoFactorAuthentication([FromBody] VerifyTwoFactorAuthenticationViewModel model)
        {
            try
            {
                return Ok( await twoFactorAuthentication.VerifyTwoFactorAuthentication(model));
             }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        
        
        
        }


    }
}
