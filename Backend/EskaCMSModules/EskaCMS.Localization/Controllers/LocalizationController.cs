using EskaCMS.Core.Extensions;
using EskaCMS.Localization.Models;
using EskaCMS.Localization.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Localization.Controllers.Controllers
{

    [ApiController]
    [Route("api/Localization")]
    [Authorize]
    public class LocalizationController : ControllerBase
    {
        private readonly ILocalizationServices _localizationServices;
        private readonly IWorkContext _workContext;

        public LocalizationController(ILocalizationServices localizationServices, IWorkContext workContext)
        {
            _localizationServices = localizationServices;
            _workContext = workContext;
        }

        [HttpPost]
        [Route("GetSlugLocalization")]
        public async Task<IActionResult> GetSlugLocalization(SlugObjVM slugObj)
        {

            try
            {

          
                var SiteId = _workContext.GetCurrentSiteId();
                var CultureId = _workContext.GetCurrentCultureId();
                return Ok(await _localizationServices.GetLocalization(slugObj.Slug, SiteId, CultureId));

               
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }


        }


        [HttpPost]
        [Route("ScanResource")]
        public async Task<IActionResult> ScanResource(ScanResourceVM obj)
        {

            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var CultureId = _workContext.GetCurrentCultureId();
                return Ok(await _localizationServices.ScanLocalization(obj.Component, obj.Key,obj.Group,obj.Slug));
            }
            catch (Exception ex)
            {
                // EF wraps the real DB failure (unique-index violation, "Data too long for column",
                // etc.) in InnerException. Returning only ex.Message surfaced the useless generic
                // "An error occurred while saving the entity changes. See the inner exception for
                // details." Walk to the innermost exception so the actual cause is reported.
                var root = ex;
                while (root.InnerException != null)
                    root = root.InnerException;

                return BadRequest(root.Message);
            }


        }
        
       




    }
}
