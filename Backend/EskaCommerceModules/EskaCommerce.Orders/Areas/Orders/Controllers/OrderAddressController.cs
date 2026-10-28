using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.Orders;
using EskaCommerce.Orders.Areas.Orders.ViewModels;
using EskaCommerce.Orders.Services.OrderAddressServices.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Orders.Areas.Orders.Controllers
{
    [Route("api/UserAddress")]
    [ApiController]
    [Authorize]
    public class OrderAddressController : ControllerBase
    {
        private readonly IUserAddressService userAddressService;

        public OrderAddressController(IUserAddressService UserAddressService)
        {
            this.userAddressService = UserAddressService;
        }

        [HttpPost("Add")]
        public async Task<IActionResult> Add([FromBody]OrderAddressViewModel model)
        {
            try
            {
             return Ok( await userAddressService.Add(model));
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        [HttpDelete("Delete/{Id}")]
        public async Task<IActionResult> Delete([FromRoute]long Id)
        {
            try
            {
                return Ok(await userAddressService.Delete(Id));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetAddress")]

        public async Task<IActionResult> GetAddress([FromQuery]long? userId ,[FromQuery] string countryId)
        {
            try
            {
                return Ok(await userAddressService.GetAddress(userId,countryId));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }
        [HttpPost("Update")]

        public async Task<IActionResult> Update([FromBody]OrderAddressViewModel model)
        {
            try
            {
             return Ok( await userAddressService.Update(model));

            }
            catch (Exception ex)
            {

               return BadRequest(ex.Message);
            }
        }
    }
}
