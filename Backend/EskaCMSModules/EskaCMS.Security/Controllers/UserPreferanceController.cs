using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.UsersAndRolesManagement;
using EskaCMS.Filters.Helpers.Interfaces;
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
    [Route("api/UserPreferences")]
    [ApiController]
    [Authorize]
    public class UserPreferencesController : ControllerBase
    {
        private readonly IUserPreferancese _ISitePreferances;
        private readonly IWorkContext _workContext;
        // private readonly IRSA_Helper _IRSA_Helper;

        public UserPreferencesController(IUserPreferancese IUserPreferances,
            IWorkContext workContext/* ,
            IRSA_Helper IRSA_Helper */)
        {
            _ISitePreferances = IUserPreferances;
            _workContext = workContext;
            // _IRSA_Helper = IRSA_Helper;
        }

        [HttpPost]
        [Route("AddUserPreferences")]
        public async Task<IActionResult> AddUserPreferences([FromBody] CreateUserPreferanceVM userPreferanceVM)
        {
            try
            {
                return Ok(await _ISitePreferances.AddNewUserPreferences(userPreferanceVM));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("GetUserPreferences/{UserId}")]
        [Route("GetUserPreferences")]
        public async Task<IActionResult> GetUserPreferences([FromRoute]long? UserId)
        {
            try
            {
                var userId = UserId.HasValue ? UserId.Value : await _workContext.GetCurrentUserId();
                return Ok(await _ISitePreferances.GetUserPreferencesByUserId(userId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("UpdateUserPreferences/{UserId}")]
        [Route("UpdateUserPreferences")]
        public async Task<IActionResult> UpdateUserPreferences([FromBody] UpdateUserPreferanceVM userPreferanceVM , [FromRoute]long? UserId)
        {
            try
            {
                var userId = UserId.HasValue ? UserId.Value : await _workContext.GetCurrentUserId();

                return Ok(await _ISitePreferances.UpdateUserPreferences(userPreferanceVM, userId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpDelete]
        [Route("Delete")]
        public async Task<IActionResult> Delete()
        {
            try
            {
                var userId =  await _workContext.GetCurrentUserId();
                var siteId =   _workContext.GetCurrentSiteId();

                return Ok(await _ISitePreferances.RemoveIfExist(userId, siteId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
