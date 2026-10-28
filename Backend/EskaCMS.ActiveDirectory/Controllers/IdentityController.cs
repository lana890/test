using EskaDCMSActiveDirectory.Interfaces;
using EskaDCMSActiveDirectory.Models;
using EskaDCMSActiveDirectory.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;

namespace EskaDCMSActiveDirectory.Controllers
{
    [Route("adapi/[controller]/[action]")]
    public class IdentityController : Controller
    {
        private IAdUserProvider provider;
        private IConfiguration config;


        public IdentityController(IAdUserProvider provider, IConfiguration config)
        {
            this.provider = provider;
            this.config = config;
        }

        [HttpGet]
        public async Task<IActionResult> GetDomainUsers()
        {
            try
            {
                List<ActiveDirectoryUser> response = await provider.GetDomainUsers();
                return Ok(response);

            }
            catch (Exception e)
            {
                if (e.InnerException is not null)
                {
                    e = e.InnerException;
                }
                return BadRequest(e.Message);
            }

        }


        [HttpGet("{search}")]
        [Authorize]
        public async Task<IActionResult> FindDomainUser([FromRoute] string search)
        {
            try
            {
                string staticUsername = config.GetValue<string>("staticUsername");
                ActiveDirectoryUser User;
                if (!string.IsNullOrEmpty(staticUsername))
                {
                    User = provider.FindDomainUser(staticUsername).Result.FirstOrDefault();

                }
                else
                {
                    await provider.Create(Request.HttpContext, config);
                    User = provider.CurrentUser;
                }

                return Ok(await provider.FindDomainUser(search));
            }
            catch (System.Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {


                string staticUsername = config.GetValue<string>("staticUsername");
                ActiveDirectoryUser User;
                if (!string.IsNullOrEmpty(staticUsername))
                {
                    User = provider.FindDomainUser(staticUsername).Result.FirstOrDefault();

                }
                else
                {
                    await provider.Create(Request.HttpContext, config);
                    User = provider.CurrentUser;
                }
                //return Ok(provider.CurrentUser);
                return Ok(User);
            }
            catch (System.Exception ex)
            {

                return BadRequest(ex.Message);
            }


        }
        [HttpPost]
        public async Task<IActionResult> ValidateAd([FromBody] loginRequestViewModel model)
        {
            try
            {
                return Ok(await provider.ValidateAd(model));
            }
            catch (System.Exception ex)
            {

                return BadRequest(ex.Message);
            }


        }


        [HttpPost]
        public async Task<IActionResult> RefreshCoreToken([FromBody] RefreshCoreTokenVM model)
        {
            try
            {
                return Ok(await provider.CoreRefreshToken(model));
            }
            catch (System.Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ADLogin([FromHeader] long siteId)
        {
            try
            {
                string staticUsername = config.GetValue<string>("staticUsername");
                ActiveDirectoryUser User;


                if (!string.IsNullOrEmpty(staticUsername))
                {
                    User = provider.FindDomainUser(staticUsername).Result.FirstOrDefault();

                }
                else
                {
                    await provider.Create(Request.HttpContext, config);
                    User = provider.CurrentUser;
                }

                var Result = await provider.Login(User, siteId);

                return Ok(Result);

            }
            catch (System.Exception ex)
            {

                return BadRequest(ex.Message + ex);
            }


        }
    
    }
}
