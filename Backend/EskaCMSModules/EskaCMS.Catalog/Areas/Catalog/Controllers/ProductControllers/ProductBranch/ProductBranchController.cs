using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Extensions;

using EskaCommerce.Catalog.Areas.Catalog.ViewModels.ProductAttribute;
using EskaCMS.Entities.Entity;
using EskaCommerce.Catalog.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using System.Net.Http;
using System.Net;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Products.ProductBranch;
using Microsoft.AspNetCore.Authorization;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductBranch.Interfaces;

namespace EskaCommerce.Catalog.Areas.Catalog.Controllers.ProductBranch
{
    [ApiController]
    [Route("api/ProductBranch")]
    [Authorize]
    public class ProductBranchController : ControllerBase
    {
        // private readonly IWorkContext _workContext;
        private readonly IProductBranchServices productBranchServices;
        public ProductBranchController(/* IWorkContext workContext, */ IProductBranchServices productBranchServices)
        {
            // _workContext = workContext;
            this.productBranchServices = productBranchServices;
        }
        [HttpPost("UpsertProductBranch")]
        public async Task<IActionResult> UpsertProductBranch([FromBody] RequestProductBranchViewModel model)
        {
            try
            {
                var result = await productBranchServices.UpsertProductBranch(model);
                return  Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet("GetProductBranchById/{productId}")]
        public async Task<IActionResult> GetProductBranchById([FromRoute]long productId)
        {
            try
            {
                var result = await productBranchServices.GetProductBranchById(productId);
            

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


    }
}
