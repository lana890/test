using System.Linq;

using EskaCommerce.Catalog.Data;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Entities.Entity;
using System;
using EskaCMS.Core.Extensions;
using System.Threading.Tasks;
using System.Collections.Generic;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.ProductAttribute;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductTemplateArea.TemplateInterfaces;

namespace EskaCommerce.Catalog.Areas.Catalog.Controllers
{
    [Area("Catalog")]
    [Authorize]
    [Route("api/ProductTemplateApi")]
    public class ProductTemplateApiController : Controller
    {
        private readonly IProductTemplateServices _productTemplateInterface;
        private readonly IWorkContext _workContext;
        public ProductTemplateApiController(IProductTemplateServices productTemplateInterface, IWorkContext workContext)
        {
            _productTemplateInterface= productTemplateInterface;
            _workContext = workContext;
        }

        [HttpGet]
        [Route("GetProductTemplate")]
        public async Task<IActionResult> GetProductTemplate(AttributeParams Params)
        {
            try
            {
                var SiteId = await _workContext.GetCurrentSiteIdAsync();
                return Ok(await _productTemplateInterface.GetProductTemplate(SiteId,Params));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
           

          

        }
        [HttpGet]
        [Route("GetProductTemplateList")]
        public async Task<IActionResult> GetProductTemplateList()
        {
            try
            {
                var SiteId = await _workContext.GetCurrentSiteIdAsync();
                return Ok(await _productTemplateInterface.GetProductTemplateList());
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }




        }

        [HttpGet]
        [Route("GetProductTemplateById/{Id}")]
        public async Task<IActionResult> GetProductTemplateById([FromRoute]long Id)
        {
            try
            {
                return Ok(await _productTemplateInterface.GetProductTemplateById(Id));

            }
            catch(Exception ex)
            {
                return BadRequest(ex);
            }


        }

        [HttpPost]
        [Route("AddProductTemplate")]
        public async Task<IActionResult>AddProductTemplate([FromBody] ProductTemplateViewModel model)
        {
            try
            {
                var SiteId=await _workContext.GetCurrentSiteIdAsync();  
                return Ok(await _productTemplateInterface.AddProductTemplate(model,SiteId));
            }
            catch(Exception ex)
            {
                return BadRequest(ex);
            }



        }

        [HttpPut]
        [Route("UpdateProductTemplate/{id}")]
        public async Task<IActionResult> UpdateProductTemplate(long id, [FromBody] ProductTemplateViewModel model)
        {
            try
            {
                model.Id = id;
                return Ok(await _productTemplateInterface.UpdateProductTemplate( model));

            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }


        }

        [HttpPost]
        [Route("ChangeProductTemplateStatus/{Id}/{NewStatus}")]
         public async Task<IActionResult> ChangeProductTemplateStatus(long Id, EStatus NewStatus)
        {
            try 
            {
                return Ok(await _productTemplateInterface.ChangeProductTemplateStatus(Id, NewStatus));
            }
            catch(Exception ex)
            {
                return BadRequest(ex);
            } 

        }

        [HttpGet]
        [Route("GetTemplateByCultureId/{CultureId}")]
       public async Task<IActionResult> GetTemplateByCultureId(long CultureId)
       {
            try
            {
                return Ok(await _productTemplateInterface.GetTemplateByCultureId(CultureId));
            }
            catch(Exception ex)
            {
                return BadRequest(ex);
            }
       }
    }
}
