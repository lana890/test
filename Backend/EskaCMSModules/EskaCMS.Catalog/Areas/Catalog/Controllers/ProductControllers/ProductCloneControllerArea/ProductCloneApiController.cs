using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using EskaCMS.Infrastructure.Data;
using EskaCommerce.Catalog.Services;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Services;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductCloneArea.Interfaces;

namespace EskaCommerce.Module.Catalog.Areas.Catalog.Controllers
{
    [Area("Catalog")]
    [Authorize]
    [Route("api/ProductCloneApi")]
    public class ProductCloneApiController : Controller
    {
        // private readonly IRepository<Product> _productRepository;
        // private readonly IProductServices _productService;
        // private readonly IMediaService _mediaService;
        // private readonly IWorkContext _workContext;
        private readonly IProductClone _iproduct;
        public ProductCloneApiController(/* IRepository<Product> productRepository, */ /* IProductServices productService, */ /* IMediaService mediaService, */ /* IWorkContext workContext, */ IProductClone iproduct)
        {
            // _productRepository = productRepository;
            // _productService = productService;
            // _mediaService = mediaService;
            // _workContext = workContext;
            _iproduct = iproduct;   
        }

        [HttpGet]
        [Route("GetProductClone/{id}")]
        public async Task<IActionResult> GetProductClone(long id)
        {
            try
            {
                return Ok(await _iproduct.GetProductClone(id));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }


        }

        [HttpPost]
        [Route("AddProductClone")]
        public async Task<IActionResult> AddProductClone([FromBody] ItemCloneFormVm model)
        {
            try
            {
                return Ok(await _iproduct.AddProductClone(model));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }

      
    }
}
