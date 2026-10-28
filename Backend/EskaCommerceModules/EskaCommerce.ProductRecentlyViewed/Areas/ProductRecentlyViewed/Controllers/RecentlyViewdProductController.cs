using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity.Catalog;
using EskaCommerce.Catalog.Services;
using EskaCommerce.ProductRecentlyViewed.Areas.ProductRecentlyViewed.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.ProductRecentlyViewed;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.ProductRecentlyViewed.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using EskaCMS.Infrastructure.Web.SmartTable;

namespace EskaCommerce.ProductRecentlyViewed.Areas.ProductRecentlyViewed.Controllers
{
    [ApiController]
    [Route("api/RecentlyViewdProduct")]
    [Authorize]

    public class RecentlyViewdProductController : ControllerBase
    {

        private readonly IRecentlyViewedProductServices recentlyViewedProductServices;

        public RecentlyViewdProductController(/* IWorkContext workContext, */


            IRecentlyViewedProductServices recentlyViewedProductServices)
        {
            this.recentlyViewedProductServices = recentlyViewedProductServices;
        }

    
        [HttpPost]
        [Route("GetRecentlyViewedProduct")]

        public async Task<IActionResult> GetRecentlyViewedProduct([FromBody] RecentlyRequestViewModel model)
        {
            try
            {
                return Ok(await recentlyViewedProductServices.GetRecentlyViewedProduct(model));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpPost]
        [Route("GetMostViewedProduct")]

        public async Task<IActionResult> GetMostViewedProduct([FromBody] SmartTableParam model)
        {
            try
            {
                return Ok(await recentlyViewedProductServices.GetMostViewedProduct(model));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

    }
}

