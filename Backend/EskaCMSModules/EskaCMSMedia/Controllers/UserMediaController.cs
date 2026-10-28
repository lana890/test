using EskaCMS.Core.Extensions;
using EskaCMS.Media.Models;
using EskaCMS.Media.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Media.Controllers
{
    [ApiController]
    [Route("api/UserMedia")]
    [Authorize]
    public class UserMediaController : ControllerBase
    {
        private readonly IUserMediaService _userMediaService;
        // private readonly IWorkContext _WorkContext;

        public UserMediaController(IUserMediaService userMediaService/* , IWorkContext workContext */)
        {
            _userMediaService = userMediaService;
            // _WorkContext = workContext;
        }


        [HttpPost]
        [Route("SaveUserMedia")]
        public async Task<IActionResult> SaveUserMedia(UserFilesVM userFilesVM)
        {
            try
            {
             

                return Ok(await _userMediaService.SaveUserMediaAsync(userFilesVM));
            }
            catch (Exception exc)
            {

                return BadRequest(exc);
            }
        }

        [HttpGet]
        [Route("GetUserFiles/{Username}")]
        public async Task<IActionResult> GetUserFiles(string Username)
        {
            try
            {
                return Ok(await _userMediaService.GetUserMedia(Username));
            }
            catch (Exception exc)
            {

                return BadRequest(exc);
            }
        }

        [HttpDelete]
        [Route("DeleteFile/{Id}")]
        public async Task<IActionResult> DeleteFile(long Id)
        {
            try
            {
               var result = await _userMediaService.DeleteUserFile(Id);
                if(result)
                return Ok("you files has been removed");

                return BadRequest("Failed to remove files");
            }
            catch (Exception exc)
            {

                return BadRequest(exc);
            }
        }
    }
}
