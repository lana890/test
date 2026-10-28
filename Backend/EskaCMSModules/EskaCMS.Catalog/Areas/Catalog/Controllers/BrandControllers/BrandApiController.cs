
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Infrastructure.Data;
using System;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.BrandVm;
using static EskaCMS.Entities.Enum.GeneralEnums;
using System.Collections.Generic;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Helpers;


using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Catalog;
using EskaCommerce.Catalog.Services.CatalogService.BrandServices.Interfaces;

namespace EskaCommerce.Catalog.Areas.Catalog.Controllers
{
    [Area("Catalog")]
    [Route("api/brands")]
    public class BrandApiController : Controller
    {
        // private readonly IRepository<Brand> _brandRepository;
        private readonly IBrandService _brandService;
        private readonly IWorkContext _workContext;

        public BrandApiController(/* IRepository<Brand> brandRepository, */ IBrandService brandService,IWorkContext workContext)
        {
            // _brandRepository = brandRepository;
            _brandService = brandService;
            _workContext = workContext;
        }

        [HttpPost]
        [Route("GetAllBrand")]
        [Authorize]
        public async Task<IActionResult> GetAllBrand([FromBody] BrandParamsViewModel model)
        {
            try
            {

                long SiteId = _workContext.GetCurrentSiteId();

                return Ok(await _brandService.GetAllBrand(model, SiteId));

            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

      
        [HttpPost]
        [Route("GetAllBrandFlat/{cultureId}")]
        [Authorize]
        public async Task<IActionResult> GetAllBrandFlat([FromRoute] long cultureId)
        {
            try
            {

                long SiteId = _workContext.GetCurrentSiteId();

                return Ok(await _brandService.GetAllBrandFlat(cultureId, SiteId));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        [HttpGet]
        [Route("GetBrands")]
        [Authorize]
        public async Task<IActionResult> GetBrands()
        {
            try
            {
                long siteId = _workContext.GetCurrentSiteId();
                return Ok(await _brandService.GetBrands( siteId));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);

            }

        }



        [HttpGet]
        [Route("GetBrandById/{id}")]
        [Authorize]
        public async Task<IActionResult> GetBrandById([FromRoute]long id)
        {
            try
            {
                return Ok(await _brandService.GetBrandById(id));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }

        }

        [HttpPost]
        [Route("AddBrand")]
        [Authorize]
        public async Task<IActionResult> AddBrand([FromBody] BrandViewModel model)
        {
            try
            {
                long UserId =await  _workContext.GetCurrentUserId();
                long SiteId = _workContext.GetCurrentSiteId();
               
                return Ok(await _brandService.AddBrand(model, UserId, SiteId));
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.ThrowDeepException(ex);
                if (ex.Message.Contains("IX_Catalog_Brand_SiteId_BrandsCultureId_SlugInfoId"))
                {
                    return BadRequest("Slug already used in Brand");
                }

               
                
                    return BadRequest(ex.Message);
                
            }
        }
        [HttpPost]
        [Route("UpdateBrand")]
        [Authorize]
        public async Task<IActionResult> UpdateBrand([FromBody] BrandViewModel model)
        {
            try
            {
                long UserId = await _workContext.GetCurrentUserId();
                long SiteId = _workContext.GetCurrentSiteId();

                return Ok(await _brandService.UpdateBrand(model, UserId, SiteId));
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.ThrowDeepException(ex);
                if (ex.Message.Contains("IX_Catalog_Brand_SiteId_BrandsCultureId_SlugInfoId"))
                {
                    return BadRequest("Slug already used in Brand");
                }



                return BadRequest(ex.Message);

            }
        }


        [HttpDelete]
        [Route("ChangeBrandStatus/{id}/{Status}")]
        [Authorize]
        public async Task<IActionResult> ChangeBrandStatus([FromRoute]long id, [FromRoute]  EStatus Status)
        {
            try
            {
                long UserId = await _workContext.GetCurrentUserId();


                return Ok(await _brandService.ChangeBrandStatus(id, Status, UserId));
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);

            }

        }

    


      
    }
}
