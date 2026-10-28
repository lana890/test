using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Areas.Core.Controllers
{
    [Route("api/RestrictedWord")]
    [ApiController]
    [Authorize]
    public class RestrictedWordController : Controller
    {
        private readonly IWorkContext _workContext;
        private readonly IRestrictedWordsServices _restrictedWordsServices;
        public RestrictedWordController(IWorkContext workContext, IRestrictedWordsServices restrictedWordsServices)
        {
            _workContext = workContext;
            _restrictedWordsServices = restrictedWordsServices;
        }

        [HttpPost]
        [Route("UpsertRestrictedWord")]
        public async Task<IActionResult> UpsertRestrictedWord([FromBody] RestrictedWordVM Model)
        {
            try
            {

                return Ok(await _restrictedWordsServices.UpsertRestrictedPasswords(Model));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }

        [HttpPost]
        [Route("IsRestrictedWord")]
        public async Task<IActionResult> IsRestrictedWord([FromBody] IsRestrictedWordVM Model)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var result = await _restrictedWordsServices.IsRestrictedWord(Model.Text, SiteId);
                return Ok(new {IsRestrictedWord = result});
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }

        [HttpPost]
        [Route("GetAllRestrictedWords")]

        public async Task<IActionResult> GetAllRestrictedWords([FromBody] SmartSearchTableParam<GetAllRestrictedWordFilter> model)
        {
            try
            {
                var result = await _restrictedWordsServices.AllRestrictedWords(model);

                return Ok(result);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("DeleteRestrictedWord/{Id}")]

        public async Task<IActionResult> DeleteRestrictedWord([FromRoute] long Id)
        {
            try
            {
                var result = await _restrictedWordsServices.UpdateRestrictedWordStatus(Id,EStatus.Deleted);

                if (result)
                {
                    return Ok("The restricted word has been deleted");

                }

                return BadRequest("Failed to remove restricted word");


            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
