using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Core.Areas.Core.ViewModels;
using System;
using System.Threading.Tasks;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.ProductAttribute;
using System.Collections.Generic;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Infrastructure.Helpers;

using EskaCMS.Entities.Entity;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductAttributeGroupArea.Interfaces;

namespace EskaCommerce.Catalog.Areas.Catalog.Controllers
{
    [Area("Catalog")]
    //[Authorize]
    [Route("api/ProductAttributeGroupApi")]
    public class ProductAttributeGroupApiController : Controller
    {
        private readonly IProductAttributeGroup _AttributeGroupServices;
        public ProductAttributeGroupApiController( IProductAttributeGroup AttributeGroupServices)
        {
            _AttributeGroupServices = AttributeGroupServices;
        }

        [HttpGet]
        [Route("GetProductAttributeGroups")]
        public async Task<IActionResult> GetProductAttributeGroups(AttributeParams attributeParams)
        {
            try
            {
                return Ok(await _AttributeGroupServices.GetProductAttributeGroups(attributeParams));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
           
        }

        [HttpGet]
        [Route("GetAllGroupsFlat")]
        public async Task<IActionResult> GetGroupsFlat()
        {
            try
            {
                return Ok(await _AttributeGroupServices.GetProductAttributeGroupsFlat());
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }



        [HttpGet]
        [Route("GetProductAttributeGroupById/{Id}")]
        public async Task<IActionResult> GetProductAttributeGroupById(long Id)
        {
            try
            {
                return Ok(await _AttributeGroupServices.GetProductAttributeGroupById(Id));

            }
            catch(Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost]
        [Route("AddProductAttributeGroup")]
        public async Task<IActionResult> AddProductAttributeGroup([FromBody] AtrtributeGroupViewModel model)
        {
            try
            {
                return Ok(await _AttributeGroupServices.AddProductAttributeGroup(model));
            }
            catch(Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost]
        [Route("UpdateProductAttributeGroup")]
        public async Task<IActionResult> UpdateProductAttributeGroup([FromBody] AtrtributeGroupViewModel model)
        {
            try
            {
                return Ok(await _AttributeGroupServices.UpdateProductAttributeGroup(model));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }



        [HttpGet]
        [Route("GetAttributeWithGroup")]
        public async Task<IActionResult> GetAttributeWithGroup(long CultureId)
        {
            try
            {
                return Ok(await _AttributeGroupServices.GetAttributeWithGroup(CultureId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
       

        [HttpPost]
        [Route("ChangeAttributeGroupStatus/{Id}/{NewStatus}")]
        public async Task<IActionResult> ChangeAttributeGroupStatus(long Id,EStatus NewStatus)
        {
            try
            {
                await _AttributeGroupServices.ChangeAttributeGroupStatus(Id, NewStatus);
                return Ok();
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.ThrowDeepException(ex);
                if (ex.Message.Contains("Can not delete this group"))
                {
                    return Conflict(ex.Message);
                }

                return BadRequest(ex.Message);
            }
        }

    }
}
