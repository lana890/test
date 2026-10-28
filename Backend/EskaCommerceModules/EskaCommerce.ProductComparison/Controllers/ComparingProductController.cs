

using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;

using EskaCMS.Entities.Entity.Catalog;
using EskaCommerce.Catalog.Services;
using EskaCommerce.ProductComparison.Areas.ProductComparison.ViewModels;
using EskaCommerce.ProductComparison.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using EskaCMS.Core.Services;
using EskaCommerce.ProductComparison.Models;

namespace EskaCommerce.ProductComparison.Areas.ProductComparison.Controllers
{
    [Route("api/ProductComparison")]
    [Authorize]
    [ApiController]
    public class ComparingProductController : ControllerBase
    {
        private readonly IComparingProductService _comparingProductService;


        public ComparingProductController(
            IComparingProductService comparingProductService)
           
        {
            _comparingProductService = comparingProductService;
           
        }

        [HttpPost("AddToComparison")]
        public async Task<IActionResult> AddToComparison([FromBody] AddToComparisonModel model)
        {
            try
            {
                return Ok(await _comparingProductService.AddToComparison(model));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("Remove/{Id}")]
        public async Task<IActionResult> Remove([FromRoute]long Id)
        {
            try
            {
                return Ok(await _comparingProductService.Remove(Id));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetCompareProducts")]
        public async Task<IActionResult> GetCompareProducts()
        {
            try
            {
                return Ok(await _comparingProductService.GetCompareProducts());
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
}


