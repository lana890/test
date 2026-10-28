using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Security.Controllers
{
    [Route("api/admin/WorkingHours")]
    [ApiController]
    //[Authorize]
    public class UserWorkingHoursController : ControllerBase

    {
        private readonly IUserWorkingHoursService _userWorkingHoursService;

        public UserWorkingHoursController(IUserWorkingHoursService userWorkingHoursService)
        {
            _userWorkingHoursService = userWorkingHoursService;
        }

        [Route("GetById/{Id}")]
        [HttpGet]
        public async Task<IActionResult> GetById([FromRoute]long Id)
        {
            try
            {
                var Query = await _userWorkingHoursService.GetById(Id);

                return Ok( Query);

            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [Route("GetAllWorkingHourse")]
        [HttpPost]
        public async Task<IActionResult> GetAllWorkingHourse(PaginationFiltersVM Params)
        {
            try
            {

                return Ok(await _userWorkingHoursService.GetAllWorkingHourse(Params));

            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }



        [Route("upsertWorkingHours")]
        [HttpPost]
        public async Task<IActionResult> upsert([FromBody] UpsertWorkingHoursVm workingHours)
        {
             try
                {
                var Query = await _userWorkingHoursService.upsert(workingHours);
                return Ok(Query);
                }

            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }

        [Route("IsInWorkingHours/{UserId}/{SiteId}")]
        [HttpPost]
        public async Task<IActionResult> IsInWorkingHours([FromRoute]long UserId, [FromRoute] long SiteId)
        {
            try
            {
                var Query = await _userWorkingHoursService.IsInWorkingHours(UserId, SiteId);


                return Ok(Query);

            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [Route("Remove/{Id}")]
        [HttpDelete]

        public async Task<IActionResult> Remove([FromRoute] long Id)
        {
            try
            {
                var Query = await _userWorkingHoursService.Remove(Id);


                return Ok(Query);

            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }


    }



}

