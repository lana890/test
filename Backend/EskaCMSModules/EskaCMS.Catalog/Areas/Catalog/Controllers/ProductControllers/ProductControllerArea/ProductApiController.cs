using EEskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Helpers;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Products.product.request;
using EskaCommerce.Catalog.Services;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductArea.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Areas.Catalog.Controllers.ProductControllers.ProductController
{
    [ApiController]

    [Route("api/ProductApi")]
    [Authorize]
    public class ProductApiController : ControllerBase
    {
        public IProductServices _ProductService { get; }

        public ProductApiController(IProductServices productService)
        {
            _ProductService = productService;
        }

        [HttpPost]
        [Route("GetAllProducts")]
        public async Task<IActionResult> GetProducts(SearchParamsViewModel Products)
        {

            try
            {
                var result = await _ProductService.GetProducts(Products);
                return Ok(result);


            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }


        }
        [HttpPost]
        [Route("AddProduct")]
        public async Task<IActionResult> AddProduct([FromBody] ProductRequestViewModel model)
        {
            try
            {

                return Ok(await _ProductService.AddProduct(model));

            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.ThrowDeepException(ex);
                if (ex.Message.Contains("IX_Catalog_Product_SlugInfoId_CultureId_SiteId"))
                {
                    return BadRequest("Slug Already Used In Products");
                }
                return BadRequest(ex.Message);

            }
        }
        [HttpPost]
        [Route("UpdateProduct")]
        public async Task<IActionResult> UpdateProduct([FromBody] ProductRequestViewModel model)
        {
            try
            {
                return Ok(await _ProductService.UpdateProduct(model));

            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.ThrowDeepException(ex);
                if (ex.Message.Contains("IX_Catalog_Product_SlugInfoId_CultureId_SiteId"))
                {
                    return BadRequest("Slug Already Used In Products");
                }
                return BadRequest(ex.Message);

            }
        }
        [HttpPost]
        [Route("GetProductsByCategoryId")]
        public async Task<IActionResult> GetProductByCategoryId([FromBody] ProductSearchViewModel productSearch)
        {
            try
            {
                return Ok(await _ProductService.GetProductByCategoryId(productSearch));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpPut]
        [Route("ChangeStatus/{Id}/{Status}")]
        public async Task<IActionResult> ChangeStatus([FromRoute] long Id, [FromRoute] EStatus Status)
        {
            try
            {
                return Ok(await _ProductService.ChangeStatus(Id, Status));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

        [HttpPost]
        [Route("GetProductById/{Id}")]
        public async Task<IActionResult> GetProductById([FromRoute] long Id)
        {
            try
            {
                return Ok(await _ProductService.GetProductById(Id));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetProductPriceType")]
        public async Task<IActionResult> GetProductPriceType()
        {
            try
            {
                var model = EnumHelper.ToDictionary(typeof(EPriceType)).Select(x => new { Id = x.Key, Name = x.Value });
                return Ok(model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("GetProductsFlat/{cultureId}/{categoryId}")]
        [Route("GetProductsFlat/{cultureId}")]
        public async Task<IActionResult> GetAllProductFlat([FromRoute] long cultureId, [FromRoute] long categoryId=0)
        {
            try
            {

                return Ok(await _ProductService.GetAllProductFlat(cultureId, categoryId));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);

            }
        }


        [HttpGet]
        [Route("GetProductsForAdmin")]
        public async Task<IActionResult> GetProductsForAdmin()
        {

            try
            {
                return Ok(await _ProductService.GetProductsForAdmin());
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

     


        [HttpPost]
        [Route("GetAllProductsByCategoryIdsForAdmin")]
        public async Task<IActionResult> GetAllProductsByCategoryIdsForAdmin([FromBody] long[] Ids)
        {

            try
            {

                return Ok(await _ProductService.GetAllProductsByCategoryIdsForAdmin(Ids));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        [Route("UpsertProductCategoryOrders")]
        public async Task<IActionResult> UpsertProductCategoryOrders([FromBody] UpsertProductCategoryOrderDto model )
        {

            try
            {

                return Ok(await _ProductService.UpsertProductCategoryOrders(model));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("DeleteProductCategory")]
        public async Task<IActionResult> DeleteProductCategory([FromBody] DeleteProductCategoryDto model)
        {

            try
            {

                var result = await _ProductService.DeleteProductCategory(model);

               
                  return Ok("The Product has been deleted from this category");

                

               
               
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

    



    }
}
