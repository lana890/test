using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore;
using System.Threading.Tasks;
using EskaCMS.Core.Extensions;

namespace EskaCMS.UserNavigationTracer.Controllers
{
    [Route("api/UserTracer")]
    [ApiController]
    [Authorize]
    public class UserTracerController : ControllerBase
    {
        private readonly IWorkContext _workContext;


        public UserTracerController( IWorkContext workContext)
        {
            _workContext = workContext;
        }


        [HttpGet]
        [Route("GetSharedElements/{CultureId?}")]
        public async Task<IActionResult> GetSharedElements(long? CultureId)
        {

            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                return Ok();


            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


    }
}
