using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Module.Core.Areas.Core.ViewModels;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductLinkServices.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Areas.Catalog.Controllers.ProductLinkController
{
    [ApiController]

    [Route("api/ProductLinkApis")]
    [Authorize]
    public class ProductLinkController:ControllerBase
    {
        private readonly IProductLinkServices _productLink;
        public ProductLinkController(IProductLinkServices productLink)
        {
            _productLink = productLink;
        }
        


        [HttpPost]
        [Route("UpsertProductLinks/{ProductId}/{LinkType}")]
        public async Task<IActionResult> UpsertProductLinks(List<ItemLinkVm> RelatedProducts, long ProductId, ItemLinkType linkType)
        {
            try
            {
                return Ok(await _productLink.UpsertProductLinks(RelatedProducts, ProductId, linkType));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

        [HttpPost]
        [Route("DeleteProductLinks/{ProductId}")]
        public async Task<IActionResult> DeleteProductLinks(long ProductId)
        {
            try
            {
                return Ok(await _productLink.DeleteProductLinks(ProductId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }


        [HttpPost]
        [Route("DeleteLink/{LinkId}")]
        public async Task<IActionResult> DeleteLink(long LinkId)
        {
            try
            {
                return Ok(await _productLink.DeleteLink(LinkId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }


   
      

        [HttpGet]
        [Route("GetLinkedProducts/{ProductId}/{LinkType}")]
        public async Task<IActionResult> GetLinkedProducts(long ProductId, ItemLinkType LinkType)
        {
            try
            {
                return Ok(await _productLink.GetLinkedProducts(ProductId, LinkType));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }
        [HttpPost("GetLinkedProductsForWeb/{productId}")]
        public async Task<IActionResult> GetLinkedProductsForWeb([FromBody] SmartTableParam param, [FromRoute] long productId)
        {
            try
            {
                return Ok(await _productLink.GetLinkedProductsForWeb(param, productId));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);

            }


        }

        [HttpPost]
        [Route("UpsertProductsCategory")]

        public async Task<IActionResult> UpsertProductsCategory([FromBody] ProductCategoryViewModel model)
        {
            try
            {
                return Ok(await _productLink.UpsertProductsCategory(model));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }


        [HttpGet]

        [Route("GetProductsCategory/{ParentId}")]

        public async Task<IActionResult> GetProductsCategory(long ParentId)
        {

            try
            {
                return Ok(await _productLink.GetProductsCategory(ParentId));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

    }
}
