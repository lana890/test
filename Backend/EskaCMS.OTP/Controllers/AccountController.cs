using EskaCMS.OTP.Models;
using EskaCMS.OTP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EskaCMS.OTP.Controllers
{
    [Route("api/Account")]
    public class AccountController : Controller
    {
        private readonly IOtpService _IUserService;
        private readonly IUserService _userService;

        public AccountController(IOtpService IUserService, IUserService userService)
        {
            _IUserService = IUserService;
            _userService = userService;
        }

        // POST: /Account/Login
        [HttpPost]
        [Route("login")]
        [Authorize]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            try
            {
                return Ok(await _userService.Login(model));
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Two factor auth is required"))
                {
                    return new ObjectResult(new { message = "Two factor auth is required", IsTwoFactorEnabled = true })
                    {
                        StatusCode = StatusCodes.Status207MultiStatus
                    };
                }

                return BadRequest(e.Message);
            }
        }

        [HttpPost]
        [Route("SendCode")]
        [Authorize]
        public async Task<IActionResult> SendCode([FromBody] SendCodeViewModel model)
        {
            try
            {
                await _IUserService.SendCode(model);
                return Ok();
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Authorize]
        [Route("VerifyCode")]
        public async Task<IActionResult> VerifyCode([FromBody] VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid model !");
            }
            try
            {
                await _IUserService.VerifyCode(model);

                return Ok();
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Authorize]
        [Route("ActivateUser")]
        public async Task<IActionResult> ActivateUser([FromBody] VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid model !");
            }
            try
            {
                await _IUserService.ActivateUser(model);

                return Ok();
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetUserTokenByUserIdAndPurpose/{Username}/{Purpose?}")]
        public async Task<IActionResult> GetUserTokenByUserIdAndPurpose(string Username, string Purpose = null)
        {
            try
            {
                return Ok(await _IUserService.GetUserTokenByUserIdAndPurpose(Username, Purpose));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
