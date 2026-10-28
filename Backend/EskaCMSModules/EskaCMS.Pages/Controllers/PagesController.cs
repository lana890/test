using EskaCMS.Core.Extensions;
using EskaCMS.Filters.ActionFilters;
using EskaCMS.Pages.Models;
using EskaCMS.Pages.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.APIs.Controllers
{

    [ApiController]
    [Route("api/Pages")]
    //[Authorize]

    public class PagesController : ControllerBase
    {

        private readonly IPages _pageServices;
        private readonly IWorkContext _workContext;

        public PagesController(IPages PagesService , IWorkContext workContext)
        {
            _pageServices = PagesService;
             _workContext = workContext;
        }

        //[HttpPost]
        //[Route("AddNewPage")]
        //public async Task<IActionResult> AddNewPage(PagesViewModel Page)
        //{
        //    try
        //    {
        //        Page.UserId = long.Parse(Request.Headers["UserId"]);
        //        Page.SiteId = long.Parse(Request.Headers["SiteId"].ToString());
        //        // Page.CompanyId = long.Parse(Request.Headers["CompanyId"].ToString());

        //        return Ok(await _pageServices.AddNewPage(Page));

        //    }
        //    catch (Exception exc)
        //    {
        //        return BadRequest(exc.Message);
        //    }
        //}
       

        [HttpGet]
        [Route("GetRelatedPages/{ParentId}")]
        public async Task<IActionResult> GetRelatedPages(long ParentId)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["SiteId"].ToString());

                return Ok(await _pageServices.GetRelatedPages(ParentId, SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        //[HttpPost]
        //[Route("EditPage/{PageId}")]
        //public async Task<IActionResult> EditPage(long PageId, PagesViewModel Page)
        //{
        //    try
        //    {
        //        Page.UserId = long.Parse(Request.Headers["UserId"]);
        //        Page.SiteId = long.Parse(Request.Headers["SiteId"].ToString());
        //        //Page.CompanyId = long.Parse(Request.Headers["CompanyId"].ToString());
        //        return Ok(await _pageServices.EditPage(Page));

        //    }
        //    catch (Exception exc)
        //    {
        //        return BadRequest(exc.Message);
        //    }
         
        //}


        [HttpPost]
        [Route("AddEditPage")] 
        public async Task<IActionResult> AddEditPage(List<PagesViewModel> Pages)
        {
            try
            {
                long UserId = await _workContext.GetCurrentUserId();
                long SiteId = long.Parse(Request.Headers["SiteId"].ToString());
                //Page.CompanyId = long.Parse(Request.Headers["CompanyId"].ToString());
                return Ok(await _pageServices.AddEditPage(UserId, SiteId, Pages));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }


        //by sondos
        [HttpGet]
        [Route("GetComponentSettingsById/{settingsId}")]
        public async Task<IActionResult> getComponentSettingsById( string SettingsId)
        {
            try
            {
                return Ok(await _pageServices.GetComponentSettingsById( SettingsId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        //by sondos

        [HttpPost]
        [Route("FilterComponentsSettings")]
        public async Task<IActionResult> FilterComponentsSettings([FromBody] FilterComponentsSettingsVM model)
        {
            try
            {
                var filteredComponents = await _pageServices.FilterComponentsSettings(model);
                return Ok(filteredComponents);
            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        //by sondos
        [HttpPost]
        [Route("UpsertComponentSettings")]
        public async Task<IActionResult> UpsertComponentSettings([FromBody] UpsertComponentSettingsVM model)
        {
            try
            {
                return Ok(await _pageServices.AddComponentSettings(model));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        //by sondos

        [HttpPost]
        [Route("DuplicateComponentSettings")]
        public async Task<IActionResult> DuplicateComponentSettings([FromBody] DuplicateComponentSettingsVM model)
        {
            try
            {
                return Ok(await _pageServices.DuplicateComponentSettings(model));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        //by sondos

       [HttpPost]
        [Route("GetAllPagesByHostname")]
       public async Task<IActionResult> GetAllPagesByHostname([FromBody] GetAllPagesByHostnameVM  model)
        {

           try
          {
                return Ok(await _pageServices.GetAllPagesByHostname(model));
           }
           catch(Exception exc)
           {
                return BadRequest(exc.Message);
          }
       }




        [HttpPut]
        [Route("ChangeParentByCultureId/{CultureId}")]
        public async Task<IActionResult> ChangeParentByCultureId([FromRoute] long CultureId)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                
                return Ok(await _pageServices.ChangeParentByCultureId(SiteId, CultureId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }




        [HttpPost]
        [Route("UpsertPageRoles")]
        public async Task<IActionResult> UpsertPageRoles(UpdatePageRolesVM VM)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = long.Parse(Request.Headers["SiteId"].ToString());
                return Ok(await _pageServices.updatePageRoles(VM.ParentPageId, VM.PageRolesList, UserId, SiteId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

        [HttpGet]
        [Route("GetIPAddress")]
        public void GetIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                string ipAddress = string.Empty;
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipAddress=ip.ToString();
                    }
                }
            }
            catch(Exception ex)
            {
             string messsage=ex.Message;
            }
        }

    

        //Bulk update and delete
        [HttpPost]
        [Route("UpdatePagesStatus/{PagesIds}/{Status}")]
        public async Task<IActionResult> UpdatePagesStatus(string PagesIds, int Status)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = long.Parse(Request.Headers["SiteId"].ToString());
                //Page.CompanyId = long.Parse(Request.Headers["CompanyId"].ToString());
                return Ok(await _pageServices.UpdatePagesStatus(PagesIds, Status, UserId, SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        [HttpPost]
        [Route("DeletePages/{PagesIds}")]
        public async Task<IActionResult> DeletePages(string PagesIds)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = long.Parse(Request.Headers["SiteId"].ToString());
                //Page.CompanyId = long.Parse(Request.Headers["CompanyId"].ToString());
                return Ok(await _pageServices.DeletePages(PagesIds, UserId, SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        //End bulk update and delete



        
        //[HttpPost]
        //[Route("DuplicatePage/{ParentId}")]
        //public async Task<IActionResult> DuplicatePage(long ParentId)
        //{
        //    try
        //    {
        //        var UserId = long.Parse(Request.Headers["UserId"]);
        //        var SiteId = long.Parse(Request.Headers["SiteId"].ToString());
        //        return Ok(await _pageServices.DublicatePage(ParentId, SiteId, UserId));

        //    }
        //    catch (Exception exc)
        //    {
        //        return BadRequest(exc.Message);
        //    }
        //}


        [HttpPost]
        [Route("SavePageSettings")]
        public async Task<IActionResult> SavePageSettings(SavePageSettingsViewModel SavePageSettingsModel)
        {
            try
            {
                SavePageSettingsModel.UserId = await _workContext.GetCurrentUserId();
                var SiteId = long.Parse(Request.Headers["SiteId"].ToString());

                return Ok(await _pageServices.SavePageSettings(SavePageSettingsModel, SiteId));
            }

            catch (Exception exc)
            {
                return BadRequest(exc);
            }

        }

        [HttpGet]
        [Route("GetPageById/{id?}")]
        public async Task<IActionResult> GetPageById(int id)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());
                return Ok(await _pageServices.GetPageByIdAsync(id, SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }

        [HttpPost]
        [Route("GetSpecificSettingForComponent")]
        public async Task<IActionResult> GetSpecificSettingForComponent([FromBody] GetSpecificSettingsVM model)
        {
            try
            {
                var result = await _pageServices.GetSpecificSettingForPage(string.IsNullOrEmpty(model.PageName) ? model.Slug : model.PageName, model.ComponentName, model.KeyName);
                return Ok(new { Settings = result });

            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }


        [HttpGet]
        [Route("ChangePageAndTemplatesVersions")]
        public IActionResult ChangePageAndTemplatesVersions()
        {
            try
            {
                _pageServices.ChangePageAndTemplatesVersions();
                return Ok();
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }

        [HttpPost]
        [Authorize]
        [Route("GetAllPages")]
        public async Task<IActionResult> GetAllPages([FromBody]GetAllPagesSearchParamsVM getAllPagesSearchParamsVM)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());
                return Ok(await _pageServices.GetAllPages(getAllPagesSearchParamsVM,SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }
        [HttpGet] 
        [Route("GetAllPagesFlat/{PageName?}/{PageCategory?}/{Status?}/{CultureId?}")]
        public async Task<IActionResult> GetAllPagesFlat(string PageName = null, long? PageCategory = null, EStatus? Status=null, long CultureId = 0)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());
                return Ok(await _pageServices.GetAllPagesFlat(PageName, PageCategory, Status, SiteId, CultureId));
            }
            catch(Exception ex)
            {
                return BadRequest(ex);
            }

        }






        //by sondos


        [HttpPost]
        [Authorize]
        [Route("GetAllPagesByCultures")]
        public async Task<IActionResult> GetAllPagesByCultures([FromBody] long[] ids)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());
                return Ok(await _pageServices.GetAllPagesByCultures(ids));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }




        [HttpGet]
        [Authorize]
        [Route("GetPageCultures/{pageId}/{ParentPageId}")]
        public async Task<IActionResult> GetPageCultures(long ParentPageId )
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());
                return Ok(await _pageServices.GetPageCultures(ParentPageId, SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }

        [HttpGet]
        [Authorize]
        [Route("ClonePages/SourceParentId/TargetPageId")]
        public async Task<IActionResult> ClonePages(long SourceParentId,long TargetPageId)
        {
            try
            {

                var SiteId = long.Parse(Request.Headers["SiteId"].ToString());
                return Ok(await _pageServices.ClonePages(TargetPageId, SourceParentId, SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }


        [HttpGet]
        [Route("GetPageSettings/{pageId}")]
        public async Task<IActionResult> GetPageSettingsAsync(long pageId)
        {
            try
            {
                return Ok(await _pageServices.GetPageSettings(pageId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("UpdatePageVersion/{pageId}/{versionId}")]
        public IActionResult UpdatePageVersion(long pageId, long versionId)
        {
            try
            {
                _pageServices.UpdatePageVersion(pageId, versionId);
                return Ok(true);
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetAllPageVersions/{pageId}")]
        public async Task<IActionResult> GetAllPageVersions(long pageId)
        {
            try
            {
                return Ok(await _pageServices.GetAllPageVersions(pageId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        //[HttpGet]
        //[Route("GetPageVersion/{versionId}")]
        //public async Task<IActionResult> GetPageVersion(long versionId)
        //{
        //    try
        //    {
        //         return Ok(await _pageServices.GetPageVersion(versionId));
        //    }
        //    catch (Exception exc)
        //    {
        //        return BadRequest(exc.Message);
        //    }
        //}

        [HttpGet]
        [Route("DeletePageByID/{PageId}")]
        public async Task<IActionResult> DeletePageByID(long PageId)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                return Ok(await _pageServices.DeletePageByID(PageId, UserId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }



        }

        [HttpPost]
        [Route("AddFavouritePage/{PageId}")]
        [Authorize]
        public async Task<IActionResult>AddFavouritePage(long PageId)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                return Ok(await _pageServices.AddFavouritePageFroUser(UserId, PageId));

            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
        [HttpGet]
        [Route("GetAllFavouritePage")]
        [Authorize]
        public async Task<IActionResult> GetAllFavouritePage()
        {
            try
            {
                return Ok(await _pageServices.GetAllFavouritePage());   

            }
            catch(Exception ex)
            {
                return BadRequest(ex);
            }
        }
        [HttpGet]
        [Route("RemoveFavouritePage/{favouritePgaeId}")]
        public async Task<IActionResult> RemoveFavouritePage(long favouritePgaeId)
        {
            try
            {
                return Ok(await _pageServices.RemoveFavouritePage(favouritePgaeId));

            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost]
        [Authorize]
        [Route("SetLoginPage")]
        public async Task<IActionResult> SetLoginPage([FromBody] SetLoginPageVM model)
        {

            try
            {

                var result = await _pageServices.SetLoginPage(model);
                if (result)
                    return Ok();
                else
                    return BadRequest();
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetPreviewCode/{pageVersionId}")]
        public async Task<IActionResult> GetPreviewCode(long pageVersionId)
        {
            try
            {

                return Ok(await _pageServices.GetPreviewCode(pageVersionId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetPageVersionSettings/{versionId}")]
        public async Task<IActionResult> VersionSettings([FromRoute] long versionId)
        {
            try
            {

                return Ok(await _pageServices.GetVersionSettings(versionId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }

        [HttpGet]
        [Route("GetTemplateVersionSettings/{versionId}")]
        public async Task<IActionResult> TemplateVersionSettings([FromRoute] long versionId)
        {
            try
            {

                return Ok(await _pageServices.GetTemplateVersionSettings(versionId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }

        [HttpPost]
        [Authorize]
        [Route("EditAlias")]
        public async Task<IActionResult> EditAlias([FromBody] EditAliasViewModel model)
        {

            try
            {

                
                    return Ok(await _pageServices.UpdatePageVersionAlias(model));
                
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            } 
        }

        [HttpPost("UpdateSettingsByComponentSettingsId")]
        public async Task<IActionResult> UpdateSettingsByComponentSettingsId(
            [FromBody] ReplaceComponentBody model)
        {
            try
            {
                await _pageServices.ReplacePageComponent(model);
                return Ok();
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }


    }
}