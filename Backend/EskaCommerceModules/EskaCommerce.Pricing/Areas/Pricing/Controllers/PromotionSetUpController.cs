using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Pricing;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.Pricing.Model;
using EskaCommerce.Pricing.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using EskaCMS.Core.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace EskaCommerce.Pricing.Areas.Pricing.Controllers
{
    [Authorize]
    [ApiController] 

    [Route("api/PromotionSetUp")]
    public class PromotionSetUpController : ControllerBase
    {
        public IRepository<CartRule> _cartRuleRepository;
        public IServicePromotions _IServicePromotions;
        public IWorkContext _workContext;
        public PromotionSetUpController(

            IServicePromotions servicePromotions,
            IWorkContext workContext
            )
        {
            _IServicePromotions = servicePromotions;
            _workContext = workContext;


        }

        [HttpGet("GetAllAutoCartRules")]
        public async Task<IActionResult> GetAllCartRule()
        {
            try
            {
                return Ok(await _IServicePromotions.GetAllCartRule());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }


        [HttpPost("CreatePromotion")]

        public async Task<IActionResult> CreatePromotion([FromBody]RequestPromotionViewModel promotion)
        {
            try
            {
                return Ok(await _IServicePromotions.CreatePromotion(promotion));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        [HttpPut("editPromotion")]

        public async Task<IActionResult> UpdatePromotion([FromBody] RequestPromotionViewModel promotion)
        {
            try
            {
                return Ok(await _IServicePromotions.UpdatePromotion(promotion));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpDelete("deletePromotion/{promId}")]

        public async Task<IActionResult> DeletePromotion([FromRoute]long promId)
        {
            try
            {
                return Ok(await _IServicePromotions.DeletePromotion(promId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("GetPromotion/{promId}")]
        public async Task<IActionResult> GetPromotion([FromRoute] long promId)
        {
            try
            {
                return Ok(await _IServicePromotions.GetPromotoion(promId));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }
        [HttpGet("GetAllPromotions")]

        public async Task<IActionResult> GetAll(
           [FromQuery] int pageSize = 10,
           [FromQuery] int pageNumber = 0)
        {
            try
            {
            var result = await _IServicePromotions.GetAllPromotion(pageSize, pageNumber);

            return Ok(result);

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetPromotionType")]
        public IActionResult GetPromotionStatus()
        {
            var model = EnumHelper.ToDictionary(typeof(PromotionsEnum)).Select(x => new { Id = x.Key, Name = x.Value });
            return Ok(model);
        }
    }
}
