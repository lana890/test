using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.UsersAndRolesManagement;
using EskaCMS.Filters.Helpers.Interfaces;
using EskaCMS.Sites.Models;
using EskaCMS.Sites.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Sites.Controllers
{
    [Route("api/SitePreferences")]
    [ApiController]
    [Authorize]
    public class SitePreferencesController : ControllerBase
    {
        private readonly ISitePreferance _ISitePreferances;
        private readonly IWorkContext _workContext;
        // private readonly IRSA_Helper _IRSA_Helper;

        public SitePreferencesController(ISitePreferance IUserPreferances,
            IWorkContext workContext/* ,
            IRSA_Helper IRSA_Helper */)
        {
            _ISitePreferances = IUserPreferances;
            _workContext = workContext;
            // _IRSA_Helper = IRSA_Helper;
        }

        [HttpPost]
        [Route("AddSitePreferences")]
        public async Task<IActionResult> AddUserPreferences([FromBody] CreateSitePreferanceViewModel userPreferanceVM)
        {
            try
            {
                return Ok(await _ISitePreferances.AddNewSitePreferences(userPreferanceVM));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("GetSitePreferences")]
        public async Task<IActionResult> GetUserPreferences()
        {
            try
            {
                return Ok(await _ISitePreferances.GetSitePreference());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("UpdateSitePreferences")]
        public async Task<IActionResult> UpdateUserPreferences([FromBody] UpdateSitePreferanceVM userPreferanceVM )
        {
            try
            {

                return Ok(await _ISitePreferances.UpdateSitePreferences(userPreferanceVM));
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
                var siteId =   _workContext.GetCurrentSiteId();

                return Ok(await _ISitePreferances.RemoveIfExist(siteId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
