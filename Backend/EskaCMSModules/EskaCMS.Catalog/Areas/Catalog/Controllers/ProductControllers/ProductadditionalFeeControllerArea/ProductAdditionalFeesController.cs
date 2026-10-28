
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity.Catalog;
using System.Threading.Tasks;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using System;
using EskaCMS.Core.Extensions;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductFeesArea.Interfaces;

namespace EskaCommerce.Module.Catalog.Areas.Catalog.Controllers
{
    [Area("Catalog")]
    [Authorize]
    [Route("api/ProductAdditionalFees")]
    public class ProductAdditionalFeesController : Controller
    {
        private readonly IProductFeesService _ProductFeesService;
        private readonly IWorkContext _workContext;
        public ProductAdditionalFeesController(IProductFeesService ProductFeesService, IWorkContext workContext)
        {
            _ProductFeesService = ProductFeesService;
            _workContext = workContext;
        }

        [HttpGet]
        [Route("GetFeesByProductId/{ProductId}")]
        public async Task<IActionResult> GetByProductId(long ProductId)
        {
            try
            {
                return Ok(await _ProductFeesService.GetAdditionalFeesByProductId(ProductId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }
        [HttpGet]
        [Route("GetAdditionalFeesById/{id}")]
        public async Task<IActionResult> Get(long id)
        {
            try
            {
                return Ok(await _ProductFeesService.GetAdditionalFeesById(id));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        [HttpPost]
        [Route("UpsertProductFees")]
        public async Task<IActionResult> UpsertProductFees([FromBody] ProductFeesModel model)
        {
            try
            {
                var SiteId = await _workContext.GetCurrentSiteIdAsync();
                return Ok(await _ProductFeesService.UpsertProductFees(model, SiteId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

       // [HttpPut]
       //[Route("EditProductFees/{FeesId}")]
       // public async Task<IActionResult> EditProductFees(long FeesId, [FromBody] ProductFeesVM model)
       // {
       //     var SiteId = await _workContext.GetCurrentSiteIdAsync();
       //     return Ok(await _ProductFeesService.EditProductFees(FeesId,model, SiteId));
           
       // }

        [HttpDelete]
        [Route("DeleteProductFees/{id}")]
        public async Task<IActionResult> DeleteProductFees(long id)
        {
            try
            {
                return Ok(await _ProductFeesService.DeleteProductFees(id));


            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }
    }
}
