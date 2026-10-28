using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity.Catalog;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using EskaCommerce.Catalog.Areas.Catalog.Controllers;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.OptionArea.Interfaces;

namespace EskaCommerce.Module.Catalog.Controllers.ProductOptionApiControllers
{
    [ApiController]
    [Route("api/ProductOptionApis")]
    public class ProductOptionsApisController : ControllerBase
    {
        private readonly IProductOptionsServices _productOptions;

        public ProductOptionsApisController(
            IProductOptionsServices productOptions)
        {
            _productOptions = productOptions;
        }

        [HttpGet]
        [Route("GetOptions")]
        public async Task<IActionResult> GetOptions()
        {
            return Ok(await _productOptions.GetOptions());
        }

        [HttpGet]
        [Route("GetOptionsById/{id}")]
        public async Task<IActionResult> GetOptionsById(long id)
        {
            return Ok(await _productOptions.GetOptionsById(id));

        }
        [HttpGet]
        [Route("GetOptionsForComponent")]
        public async Task<IActionResult> GetOptionsForComponent()
        {
            return Ok(await _productOptions.GetOptionsForComponent());

        }

        [HttpPost]
        [Route("AddOptions")]
        public async Task<IActionResult> AddOptions(List<OptionViewModel> optionsModels)
        {
            try
            {
                return Ok(await _productOptions.AddOptions(optionsModels));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



        [HttpPost]
        [Route("UpdateOptions")]
        public async Task<IActionResult> UpdateOptions(List<OptionViewModel> optionsModels)
        {
            try
            {
                return Ok(await _productOptions.UpdateOptions(optionsModels));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpDelete]
        [Route("DeleteOptions/{id}")]
        public async Task<IActionResult> DeleteOptions(long id)
        {
            try
            {
                return Ok(await _productOptions.DeleteOptions(id));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }


        #region OptionsValue
        [HttpGet]
        [Route("GetProductOptionsValueByOptionId/{Id}")]
        public async Task<IActionResult> GetProductOptionsValueByOptionId(long Id)
        {
            try
            {
                return Ok(await _productOptions.GetProductOptionsValueByOptionId(Id));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

    

        [HttpPost]
        [Route("AddProductsOptionsValue")]
        public async Task<IActionResult> AddProductsOptionsValue([FromBody]List<ProductOptionViewModel>model)
        {
            try
            {
                return Ok(await _productOptions.AddProductsOptionsValue(model));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

        [HttpPost]
        [Route("UpdateProductsOptionsValue")]
        public async Task<IActionResult> UpdateProductsOptionsValue([FromBody] List<ValueItemForAdmin> model)
        {
            try
            {
                return Ok(await _productOptions.UpdateProductOptionsValueByValueId(model));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

        [HttpGet("OptionsDetailsWithOrderingValue/{ProductId}")]
        public async Task<IActionResult> OptionsDetailsWithOrderingValue([FromRoute] long ProductId)
        {
            try
            {
                return Ok(await _productOptions.GetProductOptionsDetailsWithOrdering(ProductId));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);

            }


        }

        #endregion;


    }
}

