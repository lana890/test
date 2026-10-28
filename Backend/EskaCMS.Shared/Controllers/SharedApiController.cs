using EskaCMS.ADIntegration.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace EskaCMS.Shared.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SharedApiController : ControllerBase
    {
        private readonly IADService _sharedService;
        public SharedApiController(IADService sharedService)
        {
            _sharedService = sharedService;
        }
        [HttpGet]
        public async Task<IActionResult> ActiveDirectoryLogin()
        {
            try
            {
                var result=  await _sharedService.ActiveDirectoryLogin();
                return Ok(result);
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        } 

    }
}