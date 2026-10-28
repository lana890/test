using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Core.Services.Interfaces;

namespace EskaCMS.Security.Controllers
{
    [Route("api/Guest/[action]")]
    //[Authorize]
    public class GuestController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IWorkContext _IWorkContext;
        private readonly ILoggedInUserService loggedInUserService;

        public GuestController(UserManager<User> userManager,
                                ITokenService tokenService,
                                IWorkContext IWorkContext,
                                ILoggedInUserService loggedInUserService
            )
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _IWorkContext = IWorkContext;
            this.loggedInUserService = loggedInUserService;
        }

        [HttpPost]

        public async Task<IActionResult> CreateUser()
        {
            try
            {
                var SiteId = _IWorkContext.GetCurrentSiteId();
              
                var SourceId =_IWorkContext.GetSource();

                User user = await CreateDumyUser(SiteId);
               
                var PrincipleToken = await _tokenService.CreateToken(user, true);

                return Ok(PrincipleToken);
            }
            catch (Exception e)
            {

                return BadRequest("Something went wrong");
            }
        }


		[HttpPost]

		public async Task<IActionResult> CreateUsers()
		{
            try
            {


                return Ok(new { levelOne =new { context = new { id = 1, name = "abdullah " } }, asd = "asdd" });
			}
			catch (Exception e)
			{

				return BadRequest("Something went wrong");
			}
		}

		[HttpPost]

		// [FromBody] is required: this controller is a plain Controller (no [ApiController]), so a complex
		// parameter binds from form / route / query and the JSON body is never read - every property arrived null.
		public async Task<IActionResult> CreateUserss([FromBody] MyViewModel myViewModel)
		{
			try
			{
                var x = myViewModel;

				return Ok(myViewModel);
			}
			catch (Exception e)
			{

				return BadRequest("Something went wrong");
			}
		}


		private async Task<User> CreateDumyUser(long SiteId)
        {

            try
            {

            var dummyEmail = string.Format("{0}@guest.EskaCMS.com", Guid.NewGuid());
            User newUser = new User
            {
                FullName = "Guest",
                Email = dummyEmail,
                UserName = dummyEmail,
                Type = EUsersTypes.GuestClient,
                CreationDate = DateTimeOffset.Now,
                CreatedById = 1,
                ClientIP = HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString(),
                Status = EStatus.Active,
                StatusDate = DateTimeOffset.Now,
                SecurityStamp = "AQAAAAEAACcQAAAAELIGzYdxN6Giv7M0KV3bOlPZanDbaOH+u71HsLZp1d8sNf0lOhnP+Kb4zRt7C1h7Dw==",


            };
            var UserToSite = new UsersSites { User = newUser, SiteId = SiteId, IsDefault = true, CultureId = null,CountryId=null };
            newUser.UserSites.Add(UserToSite);
            var userCustomerGroup = new CustomerGroupUser
            {
                CustomerGroupId = 2, // guest
                User = newUser
            };
            newUser.CustomerGroups.Add(userCustomerGroup);
            await _userManager.CreateAsync(newUser, "DCMs@123");
            await _userManager.AddToRoleAsync(newUser, ERoleTypes.Guest.ToString());

            return newUser;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


    }

	public class MyViewModel
	{
		public LevelOneViewModel? levelOne { get; set; }
		public string? asd { get; set; }
	}

	public class LevelOneViewModel
	{
		public ContextViewModel? context { get; set; }
	}

	public class ContextViewModel
	{
		public int id { get; set; }
		public string? name { get; set; }
	}
}
