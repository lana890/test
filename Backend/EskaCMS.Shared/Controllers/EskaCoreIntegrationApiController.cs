//using EskaCMS.Core.Extensions;


//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using System;
//using System.Net.Http;
//using System.Threading.Tasks;

//namespace EskaCMS.Shared.Controllers
//{
//    [Route("api/[controller]/[action]")]
//    [ApiController]
//    [Authorize]
//    public class EskaCoreIntegrationApiController : ControllerBase
//    {
//        HttpRequestMessage request = new HttpRequestMessage();
//        private readonly IWorkContext _workContext;
//        public EskaCoreIntegrationApiController(IWorkContext workContext)
//        {
//            _workContext = workContext;

//        }
//        [HttpGet]
//        public async Task<IActionResult> GetAllCompanies()
//        {
//            try
//            {
//                string token = "";
//                return Ok(await _integrationService.GetAllCompanies(token));
//            }
//            catch (Exception exc)
//            {

//                return BadRequest(exc.Message);
//            }
//        }

//        [HttpGet]
//        public async Task<IActionResult> GetAllMenus(string token)
//        {
//            try
//            {
//                return Ok(await _integrationService.GetAllMenus(token));
//            }
//            catch (Exception exc)
//            {

//                return BadRequest(exc.Message);
//            }
//        }
//        [HttpGet]
//        public async Task<IActionResult> GetAllGroups(string token)
//        {
//            try
//            {
//                return Ok(await _integrationService.GetAllGroups(token));
//            }
//            catch (Exception exc)
//            {

//                return BadRequest(exc.Message);
//            }
//        }
//        [HttpGet]
//        public async Task<IActionResult> GetAllUsers(string token)
//        {
//            try
//            {
//                return Ok(await _integrationService.GetAllUsers(token));
//            }
//            catch (Exception exc)
//            {

//                return BadRequest(exc.Message);
//            }
//        }
     
//        [HttpGet]
//        public async Task<IActionResult> GetCompanyBranches(int companyId)
//        {
//            try
//            {
//                string token = "";
//                return Ok(await _integrationService.GetBranchesByCompanyId(companyId, token));
//            }
//            catch (Exception exc)
//            {

//                return BadRequest(exc.Message);
//            }
//        }
//    }
//}