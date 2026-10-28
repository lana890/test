using EskaCommerce.DomesticShipment.SimsCreation.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace EskaCommerce.DomesticShipment.SimsCreation.Controllers
{
    [Authorize]
    [Route("api/SimsCreation/[action]")]
    [ApiController]
    public class SimsCreationController : ControllerBase
    {
        private readonly ISimsCreationServices simsCreationServices;

        public SimsCreationController(ISimsCreationServices simsCreationServices)
        {
            this.simsCreationServices = simsCreationServices;
        }
        [HttpPost("{Id}")]
        public async Task<IActionResult> CreateOrder([FromRoute] long Id)
        {
            try
            {
                return Ok(await simsCreationServices.CreateOrder(Id));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }
        [HttpGet("{Id}")]
        public async Task<IActionResult> GetOrderById([FromRoute] string Id)
        {
            try
            {
                return Ok(await simsCreationServices.GetOrderById(Id));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetAllServiceProvider()
        {
            try
            {
                return Ok(await simsCreationServices.GetAllServiceProvider());
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("Id")]
        public async Task<IActionResult> DeleteOrder([FromRoute] string Id)
        {
            try
            {
                return Ok(await simsCreationServices.DeleteOrder(Id));

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
}
