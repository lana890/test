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
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductAttributeArea.Interfaces;

namespace EskaCommerce.Catalog.Areas.Catalog.Controllers.ProductAttributeControllers
{
    [ApiController]
    [Route("api/ProductAttributeApi")]
    public class ProductAttributeController : ControllerBase
    {
        // private readonly IWorkContext _workContext;
        private readonly IProductApiAttribute _productAttribute;
        public ProductAttributeController(/* IWorkContext workContext, */ IProductApiAttribute productAttribute)
        {
            // _workContext = workContext;
            _productAttribute = productAttribute;
        }
        [HttpPost]
        [Route("AddProductAttributeValue/{ProductId}")]
        public async Task<IActionResult> AddProductAttributeValue([FromBody] List<RequestProductValueVM> productAttributeVM, [FromRoute] long ProductId)
        {
            try
            {

                return Ok(await _productAttribute.AddProductAttributeValue(productAttributeVM, ProductId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost]
        [Route("UpdateProductAttributeValue")]
        public async Task<IActionResult> UpdateProductAttributeValue([FromBody] List<GetProductValueVM> productAttributeVM)
        {
            try
            {

                return Ok(await _productAttribute.UpdateProductAttributeValue(productAttributeVM));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
        [HttpGet]
        [Route("GetAttributeValuebyProductId/{ProductId}")]
        public async Task<IActionResult> GetAttributeValuebyProductId(long ProductId)
        {
            try
            {
                return Ok(await _productAttribute.GetAttributeValuebyProductId(ProductId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
        [HttpPost]
        [Route("GetList")]
        public async Task<IActionResult> GetList([FromBody]  AttributeParams attributeParams)
        {
            try
            {
                return Ok(await _productAttribute.List(attributeParams));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }
        [HttpGet]
        [Route("GetAttributeById/{Id}")]
        public async Task<IActionResult> GetAttributeById(long Id)
        {
            try
            {
                return Ok(await _productAttribute.GetAttributeById(Id));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }


        [HttpPost]
        [Route("AddProductAttribute")]
        public async Task<IActionResult> AddProductAttribute([FromBody]ProductAttribuateRequestViewModel model)
        {
            try
            {
                return Ok(await _productAttribute.AddProductAttribute(model));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }

        }
  
        [HttpPost]
        [Route("UpdateProductAttribute/{id}")]
        public async Task<IActionResult> UpdateProductAttribute([FromBody] ProductAttribuateRequestViewModel model)
        {
            try
            {
                return Ok(await _productAttribute.UpdateProductAttribute( model));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }

        }
        [HttpPost]
        
        [Route("ChangeProductAttributeStatus/{Id}/{status}")]
        public async Task<IActionResult> ChangeProductAttributeStatus([FromRoute]long Id, [FromRoute] EStatus status)
        {
            try
            {
                return Ok(await _productAttribute.ChangeProductAttributeStatus(Id, status));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }

        }
        [HttpPost]
        [Route("GetProductAttributeValue/{ProductId}")]
        public async Task<IActionResult>GetProductAttributeValue(long ProductId)
        {
            try
            {
                return Ok(await _productAttribute.GetProductAttributeValue(ProductId));
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
                    
            }
        }
        [HttpGet]
        [Route("GetAttributeByTemplateId/{TemplateId}")]
        public async Task<IActionResult>GetAttributeByTemplateId(long TemplateId)
        {
            try
            {
                return Ok(await _productAttribute.GetAttributeByTemplateId(TemplateId));
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
     
    }
}
