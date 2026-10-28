using EskaCMS.Core.Extensions;
using EskaCMS.Core.Interfaces;
using EskaCMS.Core.Models.SitesCultures;
using EskaCMS.Sites.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace EskaCommerce.Sites.Controllers
{
    [Route("api/SiteCulture")]
    [ApiController]
    public class SiteCultureController : ControllerBase
    {

        private readonly ISiteCultureService _siteCultureService;
        private readonly IWorkContext _workContext;
        public SiteCultureController(
            ISiteCultureService siteCultureService,
            IWorkContext workContext
            )
        {
            _siteCultureService = siteCultureService;
            _workContext = workContext;
        }

        [HttpGet]
        [Authorize]
        [Route("GetCulturesBySiteId/{SiteId?}")]
        public async Task<IActionResult> GetSiteCultures(long ? SiteId)
        {
            try
            {
                return Ok(await _siteCultureService.GetSiteCultures(SiteId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Authorize]
        [Route("Add")]
        public async Task<IActionResult> Add([FromBody] SitesCulturesViewModel siteCulture)
        {
            try
            {
                return Ok(await _siteCultureService.Add(siteCulture));

            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Authorize]
        [Route("Edit")]
        public async Task<IActionResult> Edit([FromBody] SitesCulturesViewModel siteCulture)
        {
            try
            {
                return Ok(await _siteCultureService.Edit(siteCulture));

            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Authorize]
        [Route("AddRange")]
        public async Task<IActionResult> AddRange([FromBody] List<SitesCulturesViewModel> siteCultures)
        {
            try
            {
                return Ok(await _siteCultureService.AddSiteCultures(siteCultures));

            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }


        [HttpGet]
        [Authorize]
        [Route("Delete")]
        public async Task<IActionResult> Delete(long Id)
        {
            try
            {
                return Ok(await _siteCultureService.DeleteSiteCulture(Id));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Route("AddCssStyleToSiteCultures")]
        public async Task<IActionResult> AddCssStyleToSiteCultures([FromBody] SiteCultureCssStyle model)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                return Ok(await _siteCultureService.AddCssStyleToSiteCultures(model.CultureId, model.CssStyle, SiteId));
            }
            catch(Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpGet]
        [Route("GetCssStyle/{CultureId}")]
        public async Task<IActionResult> GetCssStyle(long CultureId)
        {
            try
            {
                return Ok(await _siteCultureService.GetCssStyle(CultureId));
            }


            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}