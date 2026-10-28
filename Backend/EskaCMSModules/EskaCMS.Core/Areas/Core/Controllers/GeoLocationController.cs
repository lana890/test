using EskaCMS.Core.Models.GeoLocation;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Areas.Core.Controllers
{
    [ApiController]

    [Route("api/GeoLocation")]
    [Authorize]
    public class GeoLocationController : ControllerBase
    {
        private readonly IGeoLocationServices geoLocationServices;

        public GeoLocationController(IGeoLocationServices geoLocationServices )
        {
            this.geoLocationServices = geoLocationServices;
        }

        [HttpPost("AddGeoLocation")]
        public async Task<IActionResult> AddGeoLocation()
        {
            try
            {
                await geoLocationServices.AddGeoLocation();
                return Ok();
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }


        [HttpPost("GetUsersGeoLocation")]
        public async Task<IActionResult> GetUsersGeoLocation([FromBody]GeoLocationSearchRequest model)
        {
            try
            {
               
                if (model.TypeSearch == GeoLocationEnum.City)
                {

                    return Ok(await geoLocationServices.GetUsersCities(model));

                }
                else if (model.TypeSearch == GeoLocationEnum.Area)
                {
                    return Ok(await geoLocationServices.GetUsersArea(model));
                }
                else
                {
                    return Ok(await geoLocationServices.GetUsersCountries(model.PageSize, model.PageNumber));
                }
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        
        }

       
        [HttpPost("GetUserGeoLocation")]
        public async Task<IActionResult> GetUserGeoLocation([FromBody] UserGeoLocationSearchRequest model)
        {
            try
            {
                return Ok(await geoLocationServices.GetUserGeoLocation(model));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }
    }
}
