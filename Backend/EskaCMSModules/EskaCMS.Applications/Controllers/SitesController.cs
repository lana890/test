using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models.SitesCultures;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Sites.Models;
using EskaCMS.Sites.Services.Interfaces;
using EskaCMS.Sites.Services.Model;
using EskaCMS.UserDashboard.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Sites.Controllers
{
    [Route("api/Sites")]
    [ApiController]
    public class SitesController : ControllerBase
    {

        private readonly ISites _SitesService;
        private readonly IWorkContext _workContext;
        private readonly ISiteSlugServices _siteSlugServices;
        private readonly IGeoLocationServices geoLocationServices;

        public SitesController(
            ISites SitesService,
            IWorkContext workContext,
            ISiteSlugServices siteSlugServices,

            IGeoLocationServices geoLocationServices

            )

        {

            _SitesService = SitesService;
            _workContext = workContext;
            _siteSlugServices = siteSlugServices;
            this.geoLocationServices = geoLocationServices;
        }

        [HttpGet]
        [Authorize]
        [Route("GetAllSites")]
        public async Task<IActionResult> GetAllSites()
        {
            try
            {
                long userId = await _workContext.GetCurrentUserId();
                return Ok(await _SitesService.GetAllSites(userId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }



        [HttpPost]
        //[Authorize]
        [Route("GetMetaTages")]
        public async Task<IActionResult> GetMetaTages([FromBody] MetaTagsRequestViewModel model)
        {
            try
            {
                return Ok(new { result = await _SitesService.GetMetaTages(model.Slug) });
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }





        [HttpGet]
        [Route("GetAssignedUnassignedSites/{userId}")]
        public async Task<IActionResult> GetAssignedUnassignedSites(int userId = 0)
        {
            try
            {
                if (userId != 0)
                    return Ok(await _SitesService.GetAssignedUnassignedSites(userId));
                else
                    return Ok("There is no User");
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Authorize]
        [Route("SetDefaultPage")]
        public async Task<IActionResult> SetDefaultPage([FromBody] DefaultPageObjVM DefaultPage)
        {
            try
            {
                var SiteId = int.Parse(Request.Headers["SiteId"].ToString());
                long userId = await _workContext.GetCurrentUserId();
                return Ok(await _SitesService.SetDefaultPage(SiteId, userId, DefaultPage.ParentId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        [HttpPost]
        [Route("GetSiteId")]
        public async Task<IActionResult> GetSiteId([FromBody] string HostName)
        {
            try
            {
                return Ok(await _SitesService.GetSiteId(HostName));

            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }



        [HttpPost]
        [Route("GetSiteIdByHostName")]
        public async Task<IActionResult> GetSiteIdByHostName([FromBody] string HostName)
        {
            try
            {
                return Ok(await _SitesService.GetSiteIdByHostNameSP(HostName));

            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }


        [HttpGet]
        [Route("GetHostsWithCultures/{SiteId}")]
        public async Task<IActionResult> GetHostsWithCultures(long SiteId)
        {
            try
            {
                return Ok(await _SitesService.GetSiteHostNamesWithCulture(SiteId));

            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Authorize]
        [Route("UpsertHostNames")]
        public async Task<IActionResult> UpsertHostNames([FromBody] List<SiteHostNamesWithCulture> model)
        {
            try
            {
                var userId = await _workContext.GetCurrentUserId();
                return Ok(await _SitesService.UpsertHostNamesWithCultures(model, userId));

            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }



        [HttpPost]
        [Authorize]
        [Route("ValidateHostNameExistence")]
        public async Task<IActionResult> ValidateHostNameExistence([FromBody] HostNamesValidationRequestVM model)
        {
            try
            {
                var userId = await _workContext.GetCurrentUserId();
                return Ok(await _SitesService.ValidateHostNameExistence(model, userId));

            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }


        [HttpPost]
        [Route("Upsert")]
        public async Task<IActionResult> AddSiteInfo([FromBody] SitesAddVM Site)
        {
            try
            {
                return Ok(await _SitesService.UpsertSiteInfo(Site));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Route("CompleteAddSite")]
        public async Task<IActionResult> CompleteAddSite([FromBody] SitesAddVM Site)
        {
            try
            {
                return Ok(await _SitesService.CompleteAddSite(Site));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Route("AddSiteHostName")]
        public async Task<IActionResult> AddSiteHostName([FromBody] SitesHostNamesViewModel Site)
        {
            try
            {
                return Ok(await _SitesService.AddSiteHostName(Site));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpGet]
        [Route("GetUsedSiteCultures/{siteId}")]

        public async Task<IActionResult> GetUsedSiteCultures(long? siteId)
        {
            try
            {

                var SiteId = (siteId == null || siteId == 0) ? Convert.ToInt64(Request.Headers["SiteId"]) : siteId;
                return Ok(await _SitesService.GetUsedSiteCultures(SiteId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        [HttpGet]
        [Route("GetUsedSiteCulturesByStatus/{Status?}")]
        public async Task<IActionResult> GetUsedSiteCulturesByStatus(long? Status = null)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _SitesService.GetUsedSiteCulturesByStatus(SiteId, Status));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("AddSiteCultures")]
        public async Task<IActionResult> AddSiteCultures([FromBody] List<SitesCulturesViewModel> Cultures)
        {
            try
            {
                return Ok(await _SitesService.AddSiteCultures(Cultures));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Route("EditSiteHostName")]
        public async Task<IActionResult> EditSiteHostName([FromBody] SitesHostNamesViewModel Site)
        {
            try
            {
                return Ok(await _SitesService.EditSiteHostName(Site));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Route("GetList")]
        public async Task<IActionResult> GetList([FromBody] SiteSearchTableParam<SiteListSearchVM> param)
        {
            try
            {
                return Ok(await _SitesService.GetList(param));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }



        [HttpPost]
        [Route("UpsertSiteSettings")]
        public async Task<IActionResult> UpsertSiteSettings([FromBody] SiteSettingsVM Site)
        {
            try
            {
                return Ok(await _SitesService.AddSiteSettings(Site));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }



        [HttpPost]
        [Route("AddSiteSettings")]
        public async Task<IActionResult> AddSiteSettings([FromBody] SiteSettingsVM Site)
        {
            try
            {
                return Ok(await _SitesService.AddSiteSettings(Site));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Route("EditSiteSettings")]
        public async Task<IActionResult> EditSiteSettings([FromBody] SiteSettingsVM Site)
        {
            try
            {
                return Ok(await _SitesService.EditSiteSettings(Site));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpGet]
        [Route("DeleteSiteSettings/{Id}")]
        public async Task<IActionResult> DeleteSiteSettings(string Id)
        {
            try
            {
                return Ok(await _SitesService.DeleteSiteSettings(Id));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }



        [HttpPost]
        [Route("GetSlugSettings")]
        [Authorize]
        public async Task<IActionResult> GetSlugSettings(UserNavigationVm userNavigationVm)
        {
            try
            {
                userNavigationVm.Slug = HttpUtility.UrlDecode(userNavigationVm.Slug.Split("?").FirstOrDefault());

                var PagePreviewCode = Request.Headers["PreviewCode"].ToString();
                var SlugData = new SlugInfoSettings();
                if (string.IsNullOrEmpty(PagePreviewCode))
                {
                    SlugData = await _siteSlugServices.GetSlugSettingsForWebSp(userNavigationVm);
                }
                else
                {
                    SlugData = await _siteSlugServices.GetSettingsByPreviewCodeForWebSp(PagePreviewCode);
                    if (SlugData == null)
                    {
                        return NotFound("Slug Not Found");
                    }

                    return Ok(SlugData);
                }

                if (SlugData == null)
                {
                    return NotFound("Slug Not Found");
                }


                bool CanAccessSlug = true;
                bool IsGuestUser = await _workContext.IsGuestUser();
                switch (SlugData.SlugType)
                {
                    case SlugsTypes.Page:
                        if (IsGuestUser && SlugData.PageSettings.IsLoginRequired)
                            return Conflict("/" + SlugData.PageSettings.LoginSlug);
                        CanAccessSlug = await _siteSlugServices.CanAccessSlug(SlugData.PageSettings.Id, SlugsTypes.Page);
                        break;

                    case SlugsTypes.Blog:
                        if (IsGuestUser && SlugData.PostSettings.IsLoginRequired)
                            return Conflict("/" + SlugData.PostSettings.LoginSlug);
                        CanAccessSlug = await _siteSlugServices.CanAccessSlug(SlugData.PostSettings.Id, SlugsTypes.Blog);
                        break;

                    case SlugsTypes.Product:
                        CanAccessSlug = await _siteSlugServices.CanAccessSlug(SlugData.ProductSettings.Id, SlugsTypes.Product);
                        break;

                    case SlugsTypes.Brand:
                        CanAccessSlug = await _siteSlugServices.CanAccessSlug(SlugData.BrandSettings.Id, SlugsTypes.Brand);
                        break;
                    case SlugsTypes.Category:
                        CanAccessSlug = await _siteSlugServices.CanAccessSlug(SlugData.CategoriesSettings.Id, SlugsTypes.Category);
                        break;

                }
                await geoLocationServices.AddGeoLocation();

                if (!CanAccessSlug)
                {
                    return Forbid();
                }

                return Ok(SlugData);
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetDefaultSlug")]
        [Authorize]
        public async Task<IActionResult> GetDefaultSlug()
        {
            try
            {
                return Ok(await _SitesService.GetDefaultSlug());

            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }



        [HttpPost]
        [Route("GetSlugTemplateSettings")]
        [Authorize]
        public async Task<IActionResult> GetSlugTemplateSettingsSP(NavigateToSlug Model)
        {
            try
            {
                Model.Slug = HttpUtility.UrlDecode(Model.Slug.Split("?").FirstOrDefault());


                var SlugData = await _SitesService.GetTemplateSettingsBySlugSP(Model.Slug);

                return Ok(SlugData);
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }


        [HttpPost]
        [Route("Edit")]
        public async Task<IActionResult> Edit(SitesViewModel Site)
        {
            try
            {
                //var userId = Request.Headers.FirstOrDefault(x => x.Key.ToLower() == "userid").Value.FirstOrDefault();
                var userId = await _workContext.GetCurrentUserId();

                return Ok(await _SitesService.EditSite(Site, Convert.ToInt64(userId)));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("SetSiteAsDefault")]
        public async Task<IActionResult> SetSiteAsDefault(SitesViewModel Site)
        {
            try
            {
                var userId = await _workContext.GetCurrentUserId();

                return Ok(await _SitesService.SetSiteAsDefault(Site.SiteId, Convert.ToInt64(userId)));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("SetSiteCSS")]
        public async Task<IActionResult> SetSiteCSS(SitesViewModel Site)
        {
            try
            {
                var SiteId = int.Parse(Request.Headers["SiteId"].ToString());
                var userId = await _workContext.GetCurrentUserId();
                Site.ModifiedById = userId;
                Site.SiteId = SiteId;
                return Ok(await _SitesService.SetSiteCSS(Site));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        [HttpGet]
        [Route("GetSiteCSS")]
        public async Task<IActionResult> GetSiteCSS()
        {
            try
            {
                var SiteId = int.Parse(Request.Headers["SiteId"].ToString());
                return Ok(await _SitesService.GetSiteCss(SiteId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpGet("GetThemeVariableStyle")]
        public async Task<IActionResult> GetThemeVariableStyle(int siteId)
        {
            try
            {

                var SiteId = int.Parse(Request.Headers["SiteId"].ToString());

                var content = await _SitesService.GetThemeVariableStyle(SiteId);
                if (content == null)
                    return NotFound("Theme style file not found.");

                return Content(content, "text/css");

            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpPost("setThemeVariableStyle")]
        public async Task<IActionResult> setThemeVariableStyle([FromBody] ThemeVariableViewModel model)
        {
            try
            {

                var SiteId = int.Parse(Request.Headers["SiteId"].ToString());

                 await _SitesService.setThemeVariableStyle(model.Css,SiteId);
             
                return Ok();
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpDelete]
        [Route("DeleteSite/{SiteId}")]
        public async Task<IActionResult> DeleteSite(long SiteId)
        {
            try
            {
                return Ok(await _SitesService.DeleteSite(SiteId));

            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetSiteDetails/{SiteId}")]
        public async Task<IActionResult> GetSiteDetails(long SiteId)
        {
            try
            {
                return Ok(await _SitesService.GetSiteDetails(SiteId));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }
        [HttpGet]
        [Route("DeleteSiteHost/{Id}")]
        public async Task<IActionResult> DeleteSiteHost(long Id)
        {
            try
            {
                return Ok(await _SitesService.DeleteSiteHost(Id));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("CheckSiteHosts")]
        public async Task<IActionResult> CheckSiteHosts([FromBody] SitesHostNamesViewModel host)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["SiteId"]);
                var HostName = host.HostName;

                return Ok(await _SitesService.CheckSiteHosts(SiteId, HostName));
            }
            catch (Exception exc)
            {

                return BadRequest(exc);
            }
        }
        [HttpPost]
        [Route("AddMetaDataToHostName/{siteId}")]

        public async Task<IActionResult> AddMetaDataToHostName(long siteId, List<MetaDataViewModel> metaDataViewModel)
        {
            try
            {

                return Ok(await _SitesService.AddMetaDataToHostName(siteId, metaDataViewModel));

            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }
        [HttpGet]
        [Route("GetMetaDataWithCulture/{siteId}")]

        public async Task<IActionResult> GetMetaDataWithCulture(long siteId)
        {
            try
            {
                return Ok(await _SitesService.GetMetaDataWithCulture(siteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }
        [HttpPost]
        [Route("AddSiteScripts")]
        public async Task<IActionResult> AddSiteScripts([FromBody] ScriptVM model)
        {
            try
            {
                return Ok(await _SitesService.AddSiteScripts(model.siteId, model.script));

            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }
        [HttpGet]
        [Route("GetSiteScripts/{siteId}")]
        public async Task<IActionResult> GetSiteScripts(long siteId)
        {
            try
            {
                return Ok(await _SitesService.GetSiteScripts(siteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }
    }



}