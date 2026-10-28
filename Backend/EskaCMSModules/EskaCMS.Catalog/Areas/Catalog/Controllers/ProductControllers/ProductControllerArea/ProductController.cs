using EskaCMS.Core.Data;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;

using EskaCommerce.Catalog.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Core.Events;
using EskaCMS.Entities.Enum;

using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EEskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.DataAccess.Data;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Requests;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductArea.Interfaces;

namespace EskaCommerce.Catalog.Areas.Catalog.Controllers.ProductControllers.ProductControllerArea
{
    [Route("api/product")]
    [Authorize]
    public class ProductController : ControllerBase
    {
        // private readonly IRepository<Product> _productRepository;
        // private readonly IProductPricingService _productPricingService;
        // private readonly IWorkContext _workContext;
        // private readonly IMediator _mediator;
        private readonly IProductWebServices productWebServices;
        private readonly IRepository<User> _userRepository;
        // private readonly IRepository<SitesCultures> _SitesCulturesRepository;
        // private readonly EskaDCMSDbContext _context;

        public ProductController(/* IRepository<Product> productRepository, */ /* IProductPricingService productPricingService, */
          /* IWorkContext workContext, */ /* IMediator mediator, */
          IProductWebServices productWebServices/* ,
          IRepository<SitesCultures> SitesCulturesRepository *//* , EskaDCMSDbContext context */)
        {
            // _productRepository = productRepository;
            // _productPricingService = productPricingService;
            // _workContext = workContext;
            // _mediator = mediator;
            this.productWebServices = productWebServices;
            // _SitesCulturesRepository = SitesCulturesRepository;
            // _context = context;
        }

        [HttpGet("ProductOverview/{id}")]
        public async Task<IActionResult> ProductOverview([FromRoute] long id)
        {
            try
            {
                return Ok(await productWebServices.ProductOverview(id));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }


        }
        [HttpGet("ProductDetails/{id}/{Slug}")]
        public async Task<IActionResult> ProductDetails([FromRoute] long? id = null, [FromRoute] string? Slug = null)
        {
            try
            {
                return Ok(await productWebServices.ProductDetails(id, Slug));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);

            }


        }


        [HttpGet("ViewProductDetails/{id?}/{slug?}")]
        public async Task<IActionResult> ViewProductDetails([FromRoute] long? id, [FromRoute] string? slug)
        {
            try
            {
                return Ok(await productWebServices.ViewProductDetails(id,slug));

            }
            catch (Exception ex)
            {

                return BadRequest(ex);

            }


        }

        [HttpGet("GetProductVariationDetails/{id?}/{slug?}")]

        public async Task<IActionResult> GetProductVariationDetails([FromRoute] long? id, [FromRoute] string? slug)
        {
            try
            {
                return Ok(await productWebServices.GetProductVariationDetails(id, slug));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);

            }


        }

        [HttpGet("GetProductRevieweDetails/{id?}/{slug?}")]
        public async Task<IActionResult> GetProductRevieweDetails([FromRoute] long? id, [FromRoute] string? slug)
        {
            try
            {
                return Ok(await productWebServices.GetProductRevieweDetails(id,slug));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);

            }


        }

        [HttpGet("GetProductLinksDetails/{id?}/{slug?}")]
        public async Task<IActionResult> GetProductLinksDetails([FromRoute] long? id, [FromRoute] string? slug)
        {
            try
            {
                return Ok(await productWebServices.GetProductLinksDetails(id, slug));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);

            }


        }

        [HttpGet("GetProductAttributeDetails/{id?}/{slug?}")]
        public async Task<IActionResult> GetProductAttributeDetails([FromRoute] long? id, [FromRoute] string? slug)
        {
            try
            {
                return Ok(await productWebServices.GetProductAttributeDetails(id, slug));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);

            }


        }

        [HttpGet("GetProductCategoryDetails/{id?}/{slug?}")]
        public async Task<IActionResult> GetProductCategoryDetails([FromRoute] long? id, [FromRoute] string? slug)
        {
            try
            {
                return Ok(await productWebServices.GetProductCategoryDetails(id, slug));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);

            }


        }

        [HttpGet("GetProductOptionsDetails/{id?}/{slug?}")]
        public async Task<IActionResult> GetProductOptionsDetails([FromRoute] long? id, [FromRoute] string? slug)
        {
            try
            {
                return Ok(await productWebServices.GetProductOptionsDetails(id,slug));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);

            }


        }
        [HttpPost("GetMostSellingProducts")]
        public async Task<IActionResult> GetMostSellingProducts([FromBody] ProductParamsVM model)
        {
            try
            {
                return Ok(await productWebServices.GetMostSellingProducts(model));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);

            }


        }




        [HttpPost]
        [Route("GetMappingProductByAttributeValue")]
        public async Task<IActionResult> GetMappingProductByAttributeValue([FromBody] MappingProductVM model)

        {
            try
            {
                return Ok(await productWebServices.GetMappingProductByAttributeValue(model));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);

            }


        }



        [HttpPost]
        [Route("GetProductByCategoryId")]
        public async Task<IActionResult> GetProductByCategoryId([FromBody] ProductSearchViewModel productSearch)
        {
            try
            {
                return Ok(await productWebServices.GetProductByCategoryId(productSearch));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("GetProductAttributeByCategoryId")]
        public async Task<IActionResult> GetProductAttributeByCategoryId([FromBody] FilterProductAttributeViewModel  model)
        {
            try
            {
                return Ok(await productWebServices.GetProductAttributeByCategoryId(model));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("GetProductTagsByCategoryId")]
        public async Task<IActionResult> GetProductTagsByCategoryId([FromBody] ProductTagsViewModel product)
        {
            try
            {
                return Ok(await productWebServices.GetProductTagsByCategoryId(product));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Route("GetMinAndMaxPriceProduct")]
        public async Task<IActionResult> GetMinAndMaxPriceProduct()
        {
            try
            {
                return Ok(await productWebServices.GetMinAndMaxPriceProduct());
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("GetProductsbyCategoryIdForWeb")]
        public async Task<IActionResult> GetProductsbyCategoryIdForWeb([FromBody] ProductParamswithCategoryIdVM model)
        {
            try
            {

                return Ok(await productWebServices.GetProductsbyCategoryIdForWeb(model));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("GetProductsbyCategoryIds")]
        public async Task<IActionResult> GetProductsbyCategoryIds([FromBody] ProductParamsViewModel model)
        {
            try
            {

                return Ok(await productWebServices.GetProductsbyCategoryIds(model));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("GetProductsByProductsIds")]
        public async Task<IActionResult> GetProductsbyIdsForWeb([FromBody] long[] Ids)
        {
            try
            {

                return Ok(await productWebServices.GetProductsByProductsIds(Ids));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Route("GetAllProducts")]
        [HttpPost]
        public async Task<IActionResult> GetAllProducts([FromBody] ProductFiltrationViewModel model)
        {
            try
            {
                return Ok(await productWebServices.GetAllProducts(model));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("GetProductsByIds")]
        public async Task<IActionResult> GetProductsbyIds([FromBody] ProductParamswithIdVM productParamsVM)
        {
            try
            {

                return Ok(await productWebServices.GetProductsbyIdsForWeb(productParamsVM));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        [Route("GetProductsOffers")]
        public async Task<IActionResult> GetProductsOffers([FromBody] ProductOffersVM ProductOffersVM)
        {
            try
            {
                return Ok(await productWebServices.GetTodaysOffers(ProductOffersVM));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
