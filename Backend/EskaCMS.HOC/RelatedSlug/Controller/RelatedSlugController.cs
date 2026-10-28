using EskaCMS.HOC.RelatedSlug.Services;
using EskaCMS.HOC.RelatedSlug.Services.Interfcaes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.HOC.RelatedSlug
{
    [ApiController]
    [Route("api/RelatedSlug")]
    [Authorize]
    public class RelatedSlugController : ControllerBase
    {
        private readonly IRelatedSlugServices _relatedSlugService; 
        public RelatedSlugController(IRelatedSlugServices relatedSlugService)
        {
            _relatedSlugService = relatedSlugService;   
        }

        [HttpGet]
        [Route("GetRelatedSlugsInfo/{slug}")]
        public async Task<IActionResult> GetRelatedSlugsInfo([FromRoute]string slug)
        {
            try
            {
                return Ok(await _relatedSlugService.GetRelatedSlugsInfoBySlug(slug));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

    }
}
