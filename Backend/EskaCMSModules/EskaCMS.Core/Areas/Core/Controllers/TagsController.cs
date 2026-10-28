using EskaCMS.Core.Areas.Core.ViewModels.Tags;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Entity.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Areas.Core.Controllers
{
    [ApiController]
    [Route("api/Tags/[action]")]
    [Authorize]
    public class TagsController : ControllerBase
    {
        private readonly ITagsServices tagsServices;

        public TagsController(ITagsServices tagsServices)
        {
            this.tagsServices = tagsServices;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTag(TagsEntityViewModel model)
        {
            try
            {
                return Ok(await tagsServices.CreateTag(model));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }
        [HttpPost]

        public async Task<IActionResult> UpdateTag(TagsEntityViewModel model)
        {
            try
            {
                return Ok(await tagsServices.UpdateTag(model));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }
        [HttpPost]

        public async Task<IActionResult> GetTags(SearchViewModel model)
        {
            try
            {
                return Ok(await tagsServices.GetTags(model));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }

        [HttpPost]

        public async Task<IActionResult> GetTagsByEntityType(SearchEntitesViewModel model)
        {
            try
            {
                return Ok(await tagsServices.GetTagsByEntityType(model));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }


    }
}
