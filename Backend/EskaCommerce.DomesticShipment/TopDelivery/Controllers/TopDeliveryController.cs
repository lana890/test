using EskaCMS.Entities.Entity.Orders;
using EskaCommerce.DomesticShipment.TopDelivery.Dtos;
using EskaCommerce.DomesticShipment.TopDelivery.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.DomesticShipment.TopDelivery.Controllers
{

    [Authorize]
    [Route("api/TopDelivery/[action]")]
    [ApiController]
    public class TopDeliveryController : ControllerBase
    {
        private readonly ITopDeliveryServices topDeliveryServices;

        public TopDeliveryController(ITopDeliveryServices topDeliveryServices)
        {
            this.topDeliveryServices = topDeliveryServices;
        }


        [HttpGet("{Id}")]
        public async Task<IActionResult> GetOrderHistory([FromRoute]long Id)
        {
            try
            {
                return Ok(await topDeliveryServices.GetOrderHistory(Id));
            }
            catch (Exception ex)
            {

               return BadRequest(ex.Message);
            }


        }

        [HttpPost("{ShipmentId}")]
        public async Task<IActionResult> RepushShipmentAsync([FromRoute] long ShipmentId)
        {
            try
            {
                return Ok(await topDeliveryServices.RepushShipmentAsync(ShipmentId));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }


        }
    }
}
