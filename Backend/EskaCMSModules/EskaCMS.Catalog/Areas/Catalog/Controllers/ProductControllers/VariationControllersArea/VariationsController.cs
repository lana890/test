using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Infrastructure.Helpers;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Products.options;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.VariationArea.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.Controllers.ProductControllers.VariationControllersArea
{
    [ApiController]
    [Route("api/Variations")]
    [Authorize]
    public class VariationsController : ControllerBase
    {
        private readonly IVariationServices variationServices;

        public VariationsController(IVariationServices variationServices)
        {
            this.variationServices = variationServices;
        }
        [HttpGet]
        [Route("GetProductVaration/{ProductId}")]
        public async Task<IActionResult> GetProductVaration(long ProductId)
        {
            try
            {
                return Ok(await variationServices.GetProductVaration(ProductId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }
            [HttpGet]
            [Route("GetProductVarationForAdmin/{ProductId}")]
            public async Task<IActionResult> GetProductVarationForAdmin(long ProductId)
            {
                try
                {
                    return Ok(await variationServices.GetProductVarationForAdmin(ProductId));
                }
                catch (Exception ex)
                {
                    return BadRequest(ex);
                }
            }
            [HttpPost]
        [Route("AddProductVariations")]
        public async Task<IActionResult> AddProductVariations([FromBody]VariationRequestViewModel model)
        {
            try
            {
                return Ok(await variationServices.AddProductVariations(model.Variations, model.ProductId));
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.ThrowDeepException(ex);
                return BadRequest(ex.Message);

            }
        }

        [HttpPost]
        [Route("ModifyVariationCombinations")]
        public async Task<IActionResult> ModifyVariationCombinations([FromBody] VariationRequestViewModel model)
        {
            try
            {
                return Ok(await variationServices.ModifyCombinations(model.Variations, model.ProductId));
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.ThrowDeepException(ex);
                return BadRequest(ex.Message);

            }
        }


        [HttpPost]
        [Route("EditProductVariations/{ProductId}")]
        public async Task<IActionResult> EditProductVariations([FromBody]List<VariationsViewModel> Variations,[FromRoute] long ProductId)
        {
            try
            {
                return Ok(await variationServices.EditProductVariations(Variations, ProductId));
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.ThrowDeepException(ex);

                return BadRequest(ex.Message);

            }
        }


        [HttpPost]
        [Route("ChangeOptionOrder/{ProductId}")]
        public async Task<IActionResult> ChangeOptionOrder([FromBody] List<ChangeAvailableOptionOrderVM> model, [FromRoute] long ProductId)
        {
            try
            {
                return Ok(await variationServices.ChangeOrderingForOptions(ProductId, model));
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.ThrowDeepException(ex);

                return BadRequest(ex.Message);

            }
        }

        [HttpPost]
        [Route("DeleteProductVariation/{ProductLinkId}")]
        public async Task<IActionResult> DeleteProductVariation(long ProductLinkId)
        {
            try
            {
                return Ok(await variationServices.DeleteProductVariation(ProductLinkId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }

        }

        [HttpPost]
        [Route("DeleteAllVariations/{ProductId}")]
        public async Task<IActionResult> DeleteAllVariations(long ProductId)
        {
            try
            {
                return Ok(await variationServices.DeleteAllVariations(ProductId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }

        }
        [HttpGet]
        [Route("GetProductAndVariation/{VarationId}")]
        public async Task<IActionResult> GetProductAndVariation(long VarationId)
        {
            try
            {
                return Ok(await variationServices.GetProductAndVariation(VarationId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
    }
}
