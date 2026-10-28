using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Entities.Entity.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Helpers;
using EskaCommerce.Catalog.ViewModels.Category;
using System.Collections.Generic;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Requests;
using EskaCMS.Entities.Entity.Core;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Products.product.request;
using EskaCommerce.Catalog.Services.CatalogService.CategoryServices.Interfaces;

namespace EskaCommerce.Catalog.Areas.Catalog.Controllers.CategoryControllers
{
    [Area("Catalog")]
    [Authorize]
    [Route("api/CategoriesApi")]
    public class CategoryApiController : Controller
    {
        // private readonly IRepository<Category> _categoryRepository;
        // private readonly IRepository<ProductCategory> _productCategoryRepository;
        private readonly ICategoryService _categoryService;
        //  private readonly IMediaService _mediaService;
        // private readonly IRepository<Product> _productRepository;

        public CategoryApiController(/* IRepository<Product> productRepository, */ /* IRepository<Category> categoryRepository, */ /* IRepository<ProductCategory> productCategoryRepository, */ ICategoryService categoryService /*, IMediaService mediaService*/)
        {
            // _categoryRepository = categoryRepository;
            // _productCategoryRepository = productCategoryRepository;
            _categoryService = categoryService;
            //   _mediaService = mediaService;
            // _productRepository = productRepository;
        }
        [HttpPost]
        [Route("AddCategory")]
        public async Task<IActionResult> AddCategory([FromBody]CategoryRequestViewModel model)
        {
            try
            {
                return Ok(await _categoryService.AddCategory(model));
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.ThrowDeepException(ex);
                if (ex.Message.Contains("IX_Core_Category_CultureId_SiteId_SlugInfoId"))
                {
                    return BadRequest("Slug already used in Categories");
                }

                return BadRequest($"{ex.Message}");
            }

        }
        [HttpPost]

        [Route("UpdateCategory")]
        public async Task<IActionResult> UpdateCategory([FromBody]CategoryRequestViewModel model)
        {
            try
            {
                return Ok(await _categoryService.UpdateCategory(model));
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.ThrowDeepException(ex);
                if (ex.Message.Contains("IX_Core_Category_CultureId_SiteId_SlugInfoId"))
                {
                    return BadRequest("Slug already used in Categories");
                }

                return BadRequest($"{ex.Message}");
            }

        }
        [HttpGet]
        [Route("ChangeStatus/{CatId}/{Status}")]
        public async Task<IActionResult> ChangeStatus([FromRoute]long CatId,[FromRoute] EStatus Status)
        {
            try
            {
                return Ok(await _categoryService.ChangeStatus(CatId, Status));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpGet("GetCategoriesType")]
        public IActionResult GetCategoriesType()
        {

            try
            {
                return Ok(_categoryService.GetCategoriesType());
            
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
           

        }

        [HttpPost("GetCategoriesbySiteId")]

        public async Task<IActionResult> GetCategoriesbySiteId([FromBody] CategorySearchSiteViewModel model)
        {
            try
            {
                return Ok(await _categoryService.GetCategoriesbySiteId(model));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost]
        [Route("ChangeCategoryStatusByParentId/{ParentId}/{Status}")]
        public async Task<IActionResult> ChangeCategoryStatusByParentId(long ParentId, EStatus Status)
        {
            try
            {
                return Ok(await _categoryService.ChangeCategoryStatusByParentId(ParentId, Status));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        [Route("GetAllCategoriesFlat/{CultureId}")]
        [HttpGet]
        public async Task<IActionResult> GetAllCategoriesFlat([FromRoute] long CultureId)
        {
            try
            {
            

                return Ok(await _categoryService.GetAllCategoriesFlat(CultureId));

            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);

            }
        }

        [HttpPost]
        [Route("GetAllCategories")]
        public async Task<IActionResult> GetAllCategories([FromBody] CategoryParamsViewModel model)
        {
            try
            {

                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _categoryService.GetAllCategories(model));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpGet]
        //[Authorize(Policy = Permission.Admin)]
        [Route("GetCategoriesByTypeId/{CategoryTypeId}/{CultureId}")]
        [Route("GetCategoriesByTypeId/{CategoryTypeId}")]
        public async Task<IActionResult> GetCategoriesByTypeId([FromRoute] ECategoryTypes CategoryTypeId,[FromRoute] long? CultureId = 0)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _categoryService.GetCategoriesByTypeId(CategoryTypeId, SiteId,null, CultureId, false));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpGet]
        //[Authorize(Policy = Permission.Admin)]
        [Route("GetCategoriesTreeByTypeId/{CategoryTypeId}/{CultureId?}")]
        public async Task<IActionResult> GetCategoriesTreeByTypeId(ECategoryTypes CategoryTypeId, long? CultureId = 0)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _categoryService.GetCategoriesTreeByTypeId(CategoryTypeId, SiteId, CultureId, 0));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

     
        [HttpGet]
        //[Authorize(Policy = Permission.Admin)]
        [Route("GetCategoriesById/{Id}")]
        public async Task<IActionResult> GetCategoriesById(long Id)
        {
            try
            {
                return Ok(await _categoryService.GetCategoriesById(Id));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetCategories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                return Ok(await _categoryService.GetCategories());
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }

        [HttpGet]
        [Route("GetCategoryByParentId/{ParentId}")]

        public async Task<IActionResult> GetCategoryByParentId([FromRoute] long ParentId) {

            try
            {
                return Ok(await _categoryService.GetCategoryByParentId(ParentId));
            }
            catch (Exception ex)
            {

                throw ex;
            }
        
        
        
        }


        [HttpPost("GetProductsCategory/{id}")]
        public IActionResult GetProductsCategory(long id, [FromBody] SmartTableParam param)
        {
            try
            {
                return Ok(_categoryService.GetProducts(id, param));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        [HttpPut("UpdateProductCategory/{id}")]
        public async Task<IActionResult> UpdateProductCategory(long id, [FromBody] ProductCategoryForm model)
        {
            try
            {
                await _categoryService.UpdateProductCategory(id, model);
                return Ok();
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


    }
}
