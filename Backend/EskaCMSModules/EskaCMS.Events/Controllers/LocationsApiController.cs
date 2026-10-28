using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Events.Models;
using EskaCMS.Events.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Events.Controllers
{
    [Route("api/Locations")]
    [ApiController]
    [Authorize]
    public class LocationsApiController : ControllerBase
    {
        private readonly IEventsService _eventsService;
        // private readonly IWorkContext _workContext;

        public LocationsApiController(IEventsService eventsService/* , IWorkContext workContext */)
        {
            _eventsService = eventsService;
            // _workContext = workContext;
        }

        #region Locations APIs
        [HttpPost]
        [Route("AddLocation")]
        public async Task<IActionResult> AddLocation(EventLocationVM LocationVM)
        {
            try
            {
             
                return Ok(await _eventsService.AddLocation(LocationVM));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }
        [HttpPut]
        [Route("EditLocation/{LocationId}")]
        public async Task<IActionResult> EditLocation(long LocationId, [FromBody] EventLocationVM LocationVM)
        {
            try
            {
                return Ok(await _eventsService.EditLocation(LocationVM, LocationId));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetLocations")]
        public async Task<IActionResult> GetLocations()
        {
            try
            {
                return Ok(await _eventsService.GetLocationsBySiteId());
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }


        [HttpGet]
        [Route("GetLocationsForAdmin")]
        public async Task<IActionResult> GetLocationsForAdmin()
        {
            try
            {
                return Ok(await _eventsService.GetLocationsForAdmin());
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpGet]

        [Route("GetLocationById/{LocationId}")]
        public async Task<IActionResult> GetLocationById(long LocationId)
        {
            try
            {

                return Ok(await _eventsService.GetLocationsById(LocationId));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }


        [HttpPut]
        [Route("ToggleLocationStatus/{Id}")]
        public async Task<IActionResult> ToggleLocationStatus(long Id, [FromBody] EventLocationVM locationVM)
        {
            try
            {
                return Ok(await _eventsService.ToggleLocationStatus(Id, locationVM.Status));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpDelete]
        [Route("DeleteLocation/{Id}")]
        public async Task<IActionResult> DeleteLocation(long Id)
        {
            try
            {
                var isDeleted = await _eventsService.DeleteLocation(Id);
                if (isDeleted)
                    return Ok(isDeleted);
                else
                {
                    return BadRequest("This Room is already used in a future reservation and cannot be deleted");
                }
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        #endregion
    }
}
