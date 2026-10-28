
using EskaCommerce.Zoho.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System;

using System.Threading.Tasks;


namespace EskaCommerce.Zoho.Controllers
{
    [Route("api/Zoho")]
    public class ZohoController : Controller
    {


        private readonly IZohoService _IZohoService;


        public ZohoController(IZohoService IZohoService)
        {
            _IZohoService = IZohoService;
        }


        [Route("GetAccessToken")]
        [HttpGet]
        public async Task<IActionResult> GetAccessToken()
        {
            try
            {
                //  var SiteId = long.Parse(Request.Headers["siteId"].ToString());

                return Ok(await _IZohoService.AuthorizationRequest());
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }
    }        



}
