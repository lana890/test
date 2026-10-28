using EskaCMS.OTP.Infrastructure;
using EskaCMS.OTP.Services;
using Microsoft.AspNetCore.Mvc;

namespace EskaCMS.OTP.Controllers
{
    [Route("api/Guest/[action]")]
    public class GuestController : Controller
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly IWorkContext _IWorkContext;

        public GuestController(IUserService userService,
                               ITokenService tokenService,
                               IWorkContext IWorkContext)
        {
            _userService = userService;
            _tokenService = tokenService;
            _IWorkContext = IWorkContext;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser()
        {
            try
            {
                var SiteId = _IWorkContext.GetCurrentSiteId();
                var ClientIP = HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                var user = await _userService.CreateGuestUser(SiteId, ClientIP);

                var PrincipleToken = await _tokenService.CreateToken(user, true);

                return Ok(PrincipleToken);
            }
            catch (Exception)
            {
                return BadRequest("Something went wrong");
            }
        }
    }
}
