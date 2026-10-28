using EskaCMS.HOC.SiteDuplication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.HOC.SiteDuplication.Controller
{

    [Route("api/SiteDuplication")]
    [ApiController]
    [Authorize]
    public class SiteDublicationController : ControllerBase
    {
        private readonly ISiteDuplicationService _siteDuplicationService;

        public SiteDublicationController(ISiteDuplicationService siteDuplicationService)
        {
            _siteDuplicationService = siteDuplicationService;
        }

        [HttpPost]
        [Route("DuplicatieSite/{OriginalSiteId}")]

        public async Task<IActionResult> DuplicatieSite([FromRoute] long OriginalSiteId)
        {
            try
            {
                return Ok(await _siteDuplicationService.DuplicatieSiteAsync(OriginalSiteId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }



    }
}
