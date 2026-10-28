using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.ShippingTableRate;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCommerce.ShippingTableRate.Services.Interface;
using System.Collections.Generic;
using EskaCMS.Infrastructure.Helpers;
using EskaCommerce.ShippingTableRate.Areas.ShippingTableRate.ViewModels;

namespace EskaCommerce.Shipments.Areas.ShippingTableRate.Controllers
{

    [ApiController]
    [Authorize]
    [Route("api/ShippingTableRate/[action]")]
    public class PriceAndDestinationApiController : ControllerBase
    {
        private readonly IShippingTableRateServices  shippingTableRate;

        public PriceAndDestinationApiController(IShippingTableRateServices shippingTableRate)
        {
            this.shippingTableRate = shippingTableRate;
        }

        [HttpPost]
        public async Task<IActionResult> GetAll([FromBody] SearchPriceAndDistinationViewModel model)
        {
            try
            {
                return Ok(await shippingTableRate.GetAll(model));

            }
            catch (System.Exception ex)
            {

                ex = ExceptionHelper.ThrowDeepException(ex);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                return Ok(await shippingTableRate.GetById(id));
            }
            catch (System.Exception ex)
            {

                ex = ExceptionHelper.ThrowDeepException(ex);
                return BadRequest(ex.Message);
            }
       
        }

        [HttpPost]
        public async Task<IActionResult> UpsertRate([FromBody] List<RequestRateAndDistination> model)
        {
            try
            {
                return Ok(await shippingTableRate.UpsertRate(model));
            }
            catch (System.Exception ex)
            {

                ex = ExceptionHelper.ThrowDeepException(ex);
                return BadRequest(ex.Message);
            }
        }


        [HttpDelete("{id}/{Status}")]
        public async Task<IActionResult> ChangeStatus([FromRoute]long id, [FromRoute] EStatus Status)
        {
            try
            {
                return Ok(await shippingTableRate.ChangeStatus(id, Status));
            }
            catch (System.Exception ex)
            {

                ex = ExceptionHelper.ThrowDeepException(ex);
                return BadRequest(ex.Message);
            }
        }


    }
}
