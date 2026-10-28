using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Localization.Models;
using EskaCMS.Localization.Services;
using EskaCMS.Localization.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Localization.Controllers.Controllers
{

    [ApiController]
    [Microsoft.AspNetCore.Mvc.Route("api/LocalizationApi")]
    //[Authorize]
    public class LocalizationApiController : ControllerBase
    {
        private readonly ILocalizationServices _localizationServices;
        private readonly IWorkContext _workContext;

        public LocalizationApiController(ILocalizationServices localizationServices, IWorkContext workContext)
        {
            _localizationServices = localizationServices;
            _workContext = workContext;
        }

        [HttpPost]
        [Microsoft.AspNetCore.Mvc.Route("UpsertLocalization")]
        public async Task<IActionResult> UpsertLocalization([FromBody] List<LocalizationViewModel> model)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var CultureId = _workContext.GetCurrentCultureId();
                return Ok(await _localizationServices.UpsertLocalization(model, SiteId, CultureId));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }

        [HttpPost]
        [Microsoft.AspNetCore.Mvc.Route("GetAllKeys")]
        public async Task<IActionResult> GetAllKeys([FromBody] SmartSearchTableParam<LocalizationViewModel> param)
        {
            try
            {
                return Ok(await _localizationServices.GetAllKeys(param));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }


        [HttpPost]
        [Microsoft.AspNetCore.Mvc.Route("GetComponentKeys/{ComponentName}")]
        public async Task<IActionResult> GetComponentKeys(string ComponentName)
        {
            try
            {
                return Ok(await _localizationServices.GetComponentKeys(ComponentName));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }


        [HttpPost]
        [Microsoft.AspNetCore.Mvc.Route("EditLocaliztion")]
        public async Task<IActionResult> EditLocaliztion([FromBody] LocalizationValueEditVM model)
        {
            try
            {
                return Ok(await _localizationServices.EditLocaliztion(model));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        
        }

        [HttpPost]
        [Microsoft.AspNetCore.Mvc.Route("UpdateBulkLocalizationValue")]
        public async Task<IActionResult> UpdateBulkLocalizationValue([FromBody] List<LocalizationValueEditVM> model)
        {
            try
            {
                var result = await _localizationServices.UpdateBulkLocaliztionValues(model);
                if(result)
                return Ok("The localization values has been updated");

                return Ok(" Faild to update  localization values");
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }


        //[HttpPost]
        //[Microsoft.AspNetCore.Mvc.Route("GetAllKeysByCultureId")]
        //public async Task<IActionResult> GetAllKeysByCultureId([FromBody] SmartSearchTableParam<LocalizationCultureViewModel> param)
        //{
        //    try
        //    {
        //        return Ok(await _localizationServices.GetAllKeysByCultureId(param));
        //    }
        //    catch (Exception ex)
        //    {

        //        return BadRequest(ex.Message);
        //    }

        //}



    }
}
