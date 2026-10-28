using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Core.ViewModels.LoggedInUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Areas.Core.Controllers
{

    [Route("api/LoggedInUser")]
    [ApiController]
    public class LoggedInUserController:ControllerBase
    {
        private readonly ILoggedInUserService _loggedInUserServices;

        public LoggedInUserController(ILoggedInUserService  loggedInUserService)
        {
            _loggedInUserServices = loggedInUserService;
        }


        [HttpPatch]
        [Route("ChangeLookedOutUserStatus/{Id}/{LookedOut}")]
        [Authorize]

        public async Task<IActionResult> ChangeLookedOutUserStatus([FromRoute]long Id, [FromRoute] bool LookedOut)
        {
            try
            {
                await _loggedInUserServices.ChangeLookedOutUserStatus(Id, LookedOut);
                return Ok();
            }
            catch (Exception ex)
            {

                 return BadRequest( ex.Message);
            }
        
        }

        [HttpPost]
        [Route("GetUserSessions")]
        public async Task<IActionResult> GetUserSessions([FromBody] UserNameViewModel model )
        {
            try
            {
                return Ok(await _loggedInUserServices.GetUserSessions(model.UserName));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }
        [HttpPost]
        [Route("RemoveSession/{Id}")]
        public async Task<IActionResult> RemoveSession([FromRoute] string Id)
        {
            try
            {
                return Ok(await _loggedInUserServices.RemoveSession(Id));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }
        [HttpPost]
        [Route("GetAll")]
        [Authorize]

        public async Task<IActionResult> GetAll([FromBody] SearchParamsVM searchParams)
        {
            try
            {
                return Ok(await _loggedInUserServices.GetAll(searchParams));
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        [HttpPost]
        [Route("GetLoggedInUsers/{UserId}")]
        [Authorize]

        public async Task<IActionResult>GetLoggedInUsers(long UserId)
        {
            try
            {
                return Ok(await _loggedInUserServices.GetLoggedInUsers(UserId));
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        [Route("KillSessionsOnCurrentUser/{Id}")]
        [Authorize]

        public async Task<IActionResult> KillSessionsOnCurrentUser([FromRoute] string Id)
        {
            try
            {
               await _loggedInUserServices.KillSessionsOnCurrentUser(Id);

                return Ok();
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

    }
}
