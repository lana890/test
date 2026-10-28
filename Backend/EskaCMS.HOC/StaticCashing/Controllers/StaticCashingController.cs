using EskaCMS.HOC.SiteDuplication.Services;
using EskaCMS.HOC.StaticCashing.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.HOC.SiteDuplication.Controller
{

    [Route("api/StaticCashing")]
    [ApiController]
    //[Authorize]
    public class StaticCashingController : ControllerBase
    {

        public StaticCashingController()
        {
        }

        [HttpPost]
        [Route("ScrapeSite")]

        public async Task<IActionResult> ScrapeSite()
        {
            try
            {

                ScrapingHelper ScrapingHelper = new ScrapingHelper("https://softlaunchdcms.eskadenia.com/" ,string.Empty, "wwwroot/StaticSites/Eska",2);
                await ScrapingHelper.Scrape(string.Empty);

                return Ok();
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }



    }
}
