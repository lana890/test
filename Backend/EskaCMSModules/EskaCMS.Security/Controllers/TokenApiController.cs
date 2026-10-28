using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using Microsoft.AspNetCore.Authorization;
using static EskaCMS.Core.Models.TokenVM;
using Microsoft.Extensions.Configuration;

namespace EskaCMS.Security.Controllers
{
    [Area("Core")]
    [ApiController]
    public class TokenApiController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IWorkContext _workContext;
        private readonly ILogger _logger;
        private readonly IRepository<UsersSites> _UsersSitesRepository;
        // private readonly IRepository<User> _UsersRepository;
        // private readonly IRepository<SitesHostNames> _SitesHostNameRepository;
        // private readonly IRoleOptions _RoleOptionsService;
        // private readonly RoleManager<Role> _roleManager;
        // private readonly IRepository<EskaCMS.Entities.Entity.Core.UserOptions> _UserOptionsRepository;
        public readonly IConfiguration _configuration;

        public TokenApiController(UserManager<User> userManager,
                        ILoggerFactory loggerFactory,
            IRepository<UsersSites> UsersSitesRepository,
        /* IRepository<SitesHostNames> SitesHostNameRepository, */
        /* IRoleOptions RoleOptionsService, */
                    /* RoleManager<Role> roleManager, */
                    IWorkContext workContext,
            ITokenService tokenService,
            IConfiguration configuration/* ,
           IRepository<EskaCMS.Entities.Entity.Core.UserOptions> UserOptionsRepository *//* , IRepository<User> UsersRepository */)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _UsersSitesRepository = UsersSitesRepository;
            // _UsersRepository = UsersRepository;
            // _SitesHostNameRepository = SitesHostNameRepository;
            // _RoleOptionsService = RoleOptionsService;
            _logger = loggerFactory.CreateLogger<TokenApiController>();
            // _roleManager = roleManager;
            _workContext = workContext;
            // _UserOptionsRepository = UserOptionsRepository;
            _configuration = configuration;
        }

        //[AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost("/api/token/create")]
        public async Task<IActionResult> CreateToken([FromBody] TokenVM.TokenLoginModel login, bool includeRefreshToken)
        {
            try
            {

            
            var SiteId = _workContext.GetCurrentSiteId();
                if (SiteId == 0)
                    return BadRequest("SiteId cannot be null");

            login.Username = login.Username.Trim().ToUpper();
               
            var user = await _UsersSitesRepository.Query().Where(x => (x.User.NormalizedUserName == login.Username) 
                                                                && x.SiteId == SiteId)
                                                                .Include(x => x.User).Select(x => x.User).FirstOrDefaultAsync();
            if (user != null && user.IsADAuthRequired)
            {
                return Ok(await _tokenService.CreateToken(user, includeRefreshToken));
            }
            else
            {
                if (!await _userManager.CheckPasswordAsync(user, login.Password))
                {
                    return Ok();
                }
                return Ok(await _tokenService.CreateToken(user, includeRefreshToken));
            }
            }
            catch (Exception exc)
            {

                return StatusCode(500, exc.Message);
            }
        }


        [IgnoreAntiforgeryToken]
        [HttpPost("/api/token/refresh")]
        public async Task<IActionResult> RefeshToken(TokenVM.RefreshTokenModel model)
        {
            try
            { 
            
            return Ok(await _tokenService.RefeshToken(model));
            }
            catch (Exception exc)
            {

                return StatusCode(500, exc.Message);
            }
        }

        [IgnoreAntiforgeryToken]
        [HttpPost("/api/token/refreshByCookies")]
        public async Task<IActionResult> refreshByCookies()
        {
            try
            {
               
                TokenVM.RefreshTokenModel model = new TokenVM.RefreshTokenModel()
                {
                    
                    RefreshToken = await _workContext.GetCookieValue(_configuration["Authentication:UserRefreshTokenCookieName"].ToString()),
                    Token = await _workContext.GetCookieValue(_configuration["Authentication:UserTokenCookieName"].ToString())
                };

                return Ok(await _tokenService.RefeshToken(model));
            }
            catch (Exception exc)
            {

                return StatusCode(500, exc.Message);
            }
        }

        [Authorize]
        [HttpGet("/api/token/GenerateTokenForCurrentUser")]
        public async Task<IActionResult> GenerateTokenForCurrentUser()
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();

                var userId = await _workContext.GetCurrentUserId();

                var user = await _UsersSitesRepository.Query().Where(x => x.SiteId == siteId && x.UserId == userId)
                                                                .Include(x => x.User).Select(x => x.User).FirstOrDefaultAsync();
                if (user != null)
                {
                    var result = await _tokenService.CreateToken(user, true);

                    return Ok(result);

                }
                else
                    return BadRequest("User Doesn't Exist");
            }
            catch (Exception exc)
            {

                return StatusCode(500, exc.Message);
            }
        }

        [IgnoreAntiforgeryToken]
        [HttpGet("/api/token/EncryptPassword")]
        public IActionResult EncryptPassword(string password)
        {
            try
            {
                byte[] encData_byte = new byte[password.Length];
                encData_byte = System.Text.Encoding.UTF8.GetBytes(password);
                string encodedData = Convert.ToBase64String(encData_byte);
                return Ok(encodedData);
            }
            catch (Exception ex)
            {
                throw new Exception("Error in base64Encode" + ex.Message);
            }
        }

        [IgnoreAntiforgeryToken]
        [HttpGet("/api/token/DencryptPassword")]
        public IActionResult DencryptPassword(string encodedData)
        {
            System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
            System.Text.Decoder utf8Decode = encoder.GetDecoder();
            byte[] todecode_byte = Convert.FromBase64String(encodedData);
            int charCount = utf8Decode.GetCharCount(todecode_byte, 0, todecode_byte.Length);
            char[] decoded_char = new char[charCount];
            utf8Decode.GetChars(todecode_byte, 0, todecode_byte.Length, decoded_char, 0);
            string result = new String(decoded_char);
            return Ok(result);
        }
    }
}
