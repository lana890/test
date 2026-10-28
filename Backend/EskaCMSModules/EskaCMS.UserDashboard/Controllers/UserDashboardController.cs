
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.UserDashboard.Services.Interfaces;
using EskaCMS.UserDashboard.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.UserDashboard.Controllers
{
    [Route("api/UserDashboard")]
    [ApiController]
    [Authorize]
    public class UserDashboardController : ControllerBase
    {
        private readonly IUserDashboardService _UserDashboardService;
     

        public UserDashboardController(IUserDashboardService UserDashboardService)
        {
            _UserDashboardService = UserDashboardService;
       
        }


        [HttpPost]
        [Route("GetNewAndReturningUsers")]
        public async Task<ActionResult> GetNewAndReturningUsersAsync([FromBody] GetDashBoardDataVM Data)
        {
            try
            {
                Data.SiteId = long.Parse(Request.Headers["siteId"].ToString());
                Data.FromDate = Data.FromDate == null ? DateTimeOffset.Now.AddMonths(-1) : Data.FromDate;    
                Data.ToDate = Data.ToDate == null ? DateTimeOffset.Now : Data.ToDate;

                return Ok(await _UserDashboardService.GetNewAndReturningUsers(Data));
            }
            catch (Exception e)
            {

                throw ExceptionHelper.ThrowDeepException(e);
            }
            
        }



        [HttpPost]
        [Route("GetUsersFlow")]
        public async Task<ActionResult> GetUsersFlow([FromBody] PeriodVM Data)
        {
            try
            {
                Data.FromDate = Data.FromDate == null ? DateTimeOffset.Now.AddMonths(-1) : Data.FromDate;
                Data.ToDate = Data.ToDate == null ? DateTimeOffset.Now : Data.ToDate;

                return Ok(await _UserDashboardService.GetUsersFlow(Data));
            }
            catch (Exception e)
            {

                throw ExceptionHelper.ThrowDeepException(e);
            }

        }

        [HttpGet]
        [Route("GetAllBrowsers")]
        public async Task<ActionResult> GetAllBrowsers()
        {
            try
            {

                return Ok(await _UserDashboardService.GetBrowsers());
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }

        }

        [HttpGet]
        [Route("GetUserVisits/{UserId}")]
        public async Task<ActionResult> GetUserVisits(long UserId)
        {
            try
            {
               
                return Ok(await _UserDashboardService.GetUserVisits(UserId));
            }
            catch (Exception e)
            {

                throw ExceptionHelper.ThrowDeepException(e);
            }

        }


        [HttpPost]
        [Route("GetUsersByNavigation")]
        public async Task<ActionResult> GetUsersByNavigation(UsersByVisitVM Model)
        {
            try
            {

                return Ok(await _UserDashboardService.GetUsersByNavigation(Model));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }


        [HttpPost]
        [Route("GetNavigationsByUserAndVisit")]
        public async Task<ActionResult> GetNavigationsByUserAndVisit(NavigationsByUserAndVisitVM Model)
        {
            try
            {

                return Ok(await _UserDashboardService.GetNavigationsByUserAndVisit(Model));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }



        [HttpGet]
        [Route("GetUserFlowByVisitId/{VisitId}")]
        public async Task<ActionResult> GetUserFlowByVisitId(long VisitId)
        {
            try
            {

                return Ok(await _UserDashboardService.GetUserFlowByVisitId(VisitId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }

        [HttpPost]
        [Route("GetUserNavigations")]
        public async Task<IActionResult> GetUserNavigations(UserNavigationVM userNavigationVM)
        {
            try
            { // headers
                return Ok(await _UserDashboardService.GetUserNavigations(userNavigationVM));   
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }

    }
}
