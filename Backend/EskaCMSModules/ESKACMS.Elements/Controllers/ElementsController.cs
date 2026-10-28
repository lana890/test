using EskaCMS.Elements.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore;
using System.Threading.Tasks;
using EskaCMS.Elements.ViewModels;
using EskaCMS.Core.Extensions;

namespace EskaCMS.Elements.Controllers
{
    [Route("api/Elements")]
    [ApiController]
    [Authorize]
    public class ElementsController : ControllerBase
    { 
        private IElementService _ElementService { get; set; }
        private readonly IWorkContext _workContext;


        public ElementsController(IElementService ElementService, IWorkContext workContext)
        {
            _ElementService = ElementService;
            _workContext = workContext;
        }


        [HttpGet]
        [Route("GetSharedElements/{CultureId?}")]
        public async Task<IActionResult> GetSharedElements(long? CultureId)
        {
            
            try
            {
                var SiteId =  _workContext.GetCurrentSiteId();
                //var Culture = CultureId.HasValue ? CultureId.Value :_workContext.GetCurrentCultureId();
                return Ok(await _ElementService.GetAllSheardElements(SiteId, CultureId));


            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetSharedElementsSettings/{CultureId}")]
        public async Task<IActionResult> GetSharedElementsSettings(long? CultureId = null)
        {

            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());
                var Culture = CultureId.HasValue ? CultureId.Value : _workContext.GetCurrentCultureId();
                return Ok(await _ElementService.GetAllSheardElementsSettings(SiteId, Culture));


            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



        [HttpGet]
        [Route("GetSharedElementById/{id}")]
        public async Task<IActionResult> GetSharedElementById(long id)
        {
            try
            {
                return Ok(await _ElementService.GetSharedElementById(id));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("GetSharedElementByCode")]
        public async Task<IActionResult> GetSharedElementByCode(GetElementByCodeVM Model)
        {
            try
            {
                return Ok(await _ElementService.GetElementSettingsByCode(Model.IdentifierCode, Model.CultureId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet]
        [Route("RemoveSharedElementById/{id}")]
        public async Task<IActionResult> RemoveSharedElementById(long id)
        {
            try
            {
                return Ok(await _ElementService.RemoveSharedElement(id));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        [Route("UpdateSharedElementSettings")]
        public async Task<IActionResult> UpdateSharedElementSettings([FromBody] ElementsVM Element)
        {
            try
            {
                return Ok(await _ElementService.UpdateSharedElementSettings(Element));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        [Route("ShareElement")]
        public async Task<IActionResult> ShareElement([FromBody] List<ElementsVM> Elements)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());
                return Ok(await _ElementService.ShareElements(Elements, SiteId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


    }
}
