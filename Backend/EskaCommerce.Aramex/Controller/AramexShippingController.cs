using EskaCMS.Entities.Entity.Shipments;
using EskaCommerce.Aramex.Models.Requests.Settings;
using EskaCommerce.Aramex.Models.Requests.Shipping;
using EskaCommerce.Aramex.Models.Requests.Tracking;
using EskaCommerce.Aramex.Services;
using EskaCommerce.Aramex.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Aramex.Controller
{
    [Route("api/AramexShipping")]
    [Authorize]
    public class AramexShippingController : ControllerBase
    {
        private readonly IAramexShippingService aramexShippingService;

        public AramexShippingController(IAramexShippingService aramexShippingService)
        {
            this.aramexShippingService = aramexShippingService;
        }

        [HttpPost("RepushShipment/{orderId}")]
        public async Task<IActionResult> RepushShipmentAsync([FromRoute] long orderId)
        {
            try
            {
                  return Ok(await aramexShippingService.RepushShipmentAsync(orderId));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }


        [HttpPost("CreatePickup")]

        public async Task<IActionResult> CreatePickup([FromBody] RequestPickupAdmin  model)
        {
            try
            {
                return Ok(await aramexShippingService.CreatePickup(model));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }


        }

        [HttpPost("CancelPickup/{PickupId}")]
        public async Task<IActionResult> CancelPickup([FromRoute] long PickupId)
        {
            try
            {
                return Ok(await aramexShippingService.CancelPickup(PickupId));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }

        [HttpPost("PrintLabel/{ShipmentId}")]
        public async Task<IActionResult> PrintLabel([FromRoute] long ShipmentId)
        {
            try
            {
                return Ok(await aramexShippingService.PrintLabel(ShipmentId));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);

            }

        }

        
        #region Tracking API

        [HttpPost("TrackShipments/{ShipmentId}")]

        public async Task<IActionResult> TrackShipments([FromRoute] long ShipmentId)
        {
            try
            {
                return Ok(await aramexShippingService.TrackShipments(ShipmentId));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpPost("TrackPickup/{PickupId}")]

        public async Task<IActionResult> TrackPickup([FromRoute] long PickupId)
        {
            try
            {
                return Ok(await aramexShippingService.TrackPickup(PickupId));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        #endregion


        [HttpPost("AddCitiesByAramex")]

        public async Task<IActionResult> AddCitiesByAramex()
        {

            try
            {
                return Ok(await aramexShippingService.AddCitiesByAramex());
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }
    }
}