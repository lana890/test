using EskaCommerce.Discount.Models;
using EskaCommerce.Discount.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using Microsoft.AspNetCore.Authorization;
namespace EskaCommerce.Discount.Controllers
{

    [Route("api/Discount/[action]")]
    [ApiController]
    [Authorize]
    public class DiscountController : Controller
    {

        private readonly IDiscountService _DiscountService;


        public DiscountController(IDiscountService DiscountService)
        {
            _DiscountService = DiscountService;
        }


        [HttpGet]
        public async Task<IActionResult> GetDiscountsBySiteId()
        {
            try
            {
                var siteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _DiscountService.GetDiscountsBySiteId(siteId));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get(long id)
        {
            try
            {
                var siteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _DiscountService.GetDiscount(id,siteId));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]

        public IActionResult GetDiscountTypes()
        {
            var enumVals = new List<object>();

            foreach (var item in Enum.GetValues(typeof(DiscountTypes)))
            {

                enumVals.Add(new
                {
                    id = (int)item,
                    name = item.ToString()
                });
            }

            return Ok(enumVals);


        }

        [HttpPost]
        public async Task<IActionResult> Upsert([FromBody] RequestDiscountViewModel model)
        {
            try
            {

                return Ok(await _DiscountService.UpsertDiscount(model));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }

        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                return Ok(await _DiscountService.Delete(id));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }



    }
}
