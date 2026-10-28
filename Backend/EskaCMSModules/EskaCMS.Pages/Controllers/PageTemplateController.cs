using EskaCMS.Pages.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using static BusinessModels.BusinessModel;
using Microsoft.AspNetCore.Authorization;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Pages.Models;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Pages;

namespace EskaCMS.APIs.Controllers
{

    [ApiController]
    [Route("api/PageTemplate")]
   // [Authorize]
    public class PageTemplateController : ControllerBase
    {

        private readonly IPages _pageServices;
        private readonly IWorkContext _workContext;
        public PageTemplateController(IPages PagesService, IWorkContext workContext) 
        {
            _pageServices = PagesService;
            _workContext = workContext;
        }



        [HttpPost]
        [Route("UpdatePagesTemplate/{PagesIds}/{TemplateId}")]
        public async Task<IActionResult> UpdatePagesTemplate(string PagesIds, int TemplateId)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = long.Parse(Request.Headers["SiteId"].ToString());
                //Page.CompanyId = long.Parse(Request.Headers["CompanyId"].ToString());
                return Ok(await _pageServices.UpdatePagesTemplate(PagesIds, TemplateId, UserId, SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        [HttpGet]
        [Route("GetAllPageTemplates/{Status?}/{Type?}")]
        public async Task<IActionResult> GetAllPageTemplates(EStatus? Status=null,ETemplateTypes? Type = null )
       {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());
                return Ok(await _pageServices.GetAllPageTemplates(SiteId, Status, Type));

            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }




        //[HttpGet]
        //[Route("GetActivePageTemplates")]
        //public async Task<IActionResult> GetActivePageTemplates()
        //{
        //    try
        //    {
        //        var SiteId = long.Parse(Request.Headers["siteId"].ToString());
        //        return Ok(await _pageServices.GetActivePageTemplates(SiteId));

        //    }
        //    catch (Exception exc)
        //    {
        //        return BadRequest(exc);
        //    }
        //}

        [HttpPost]
        [Route("AddPageTemplates")]
        public async Task<IActionResult> AddPageTemplates(PageTemplates pageTemplate)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());
                var UserId = await _workContext.GetCurrentUserId();

                return Ok(await _pageServices.AddPageTemplates(pageTemplate, SiteId, UserId));
            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }



        [HttpPost]
        [Route("EditPageTemplates/{PageId}")]
        public async Task<IActionResult> EditPageTemplates(long PageId, PageTemplates pageTemplate)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                return Ok(await _pageServices.EditPageTemplates(pageTemplate, UserId, PageId));
            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }


        [HttpPost]
        [Route("DuplicateTemplate/{id}")]
        public async Task<IActionResult> DuplicateTemplate(long id, PageTemplates pageTemplate)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                return Ok(await _pageServices.DuplicateTemplate(pageTemplate, id, UserId));

            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }



        [HttpDelete]
        [Route("DeleteTemplateByID/{id}")]
        public async Task<IActionResult> DeleteTemplateByID(long id)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                return Ok(await _pageServices.DeleteTemplateByID(id, UserId));
            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        [HttpGet]
        [Route("GetTemplatesVersions/{id}")]
        public async Task<IActionResult> GetTemplatesVersions(long id)
        {
            try
            {
                return Ok(await _pageServices.GetTemplatesVersions(id));
            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }
        [HttpGet]
        [Route("GetPageTemplatesById/{templateId}")]
        public async Task<IActionResult> GetPageTemplatesById(long templateId)
        {
            try
            {
                return Ok(await _pageServices.GetPageTemplateSettings(templateId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Route("SaveTemplatesSettings")]
        public async Task<IActionResult> SaveTemplatesSettings(TemplateSettingsModel tv)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());
                tv.UserId = await _workContext.GetCurrentUserId();
                return Ok(await _pageServices.SaveTemplatesSettings(tv, SiteId));
            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }
        [HttpPost]
        [Route("UpdatePageVersion/{templateId}/{versionId}")]
        public IActionResult UpdatePageVersion(long templateId, long versionId)
        {
            try
            {
                _pageServices.UpdateTemplateVersion(templateId, versionId);
                return Ok();
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Authorize]
        [Route("EditAlias")]
        public async Task<IActionResult> EditAlias([FromBody] EditAliasViewModel model)
        {

            try
            {

                
                
                    return Ok(await _pageServices.UpdateTemplateVersionAlias(model));
                
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        #region Set & Get Template Data


        [HttpPost]
        [Route("SetTemplateData")]
        public async Task<IActionResult> SetTemplateData(TemplateSettingsModel template)
        {
            try
            {
                template.UserId = await _workContext.GetCurrentUserId();

                return Ok(await _pageServices.SetTemplate(template));
            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        [HttpGet]
        [Route("GetTemplateData/{TemplateId}")]
        public async Task<IActionResult> GetTemplateData(long TemplateId)
        {
            try
            {
                var data = await _pageServices.GetTemplateData(TemplateId);
                return Ok(data);
            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        [HttpPost]
        [Authorize]
        [Route("SetTemplateCSS")]
        public async Task<IActionResult> SetTemplateCSS(TemplateSettingsModel template)
        {
            try
            {
                template.UserId = await _workContext.GetCurrentUserId();

                return Ok(await _pageServices.SetTemplate(template, UpdateTemplateEnum.CSS));
            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        [HttpGet]
        [Authorize]
        [Route("GetTemplateCSS/{TemplateId}")]
        public async Task<IActionResult> GetTemplateCSS(long TemplateId)
        {
            try
            {
                var data = await _pageServices.GetTemplateData(TemplateId);
                return Ok(new { CSS = data.Css } );
            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }




        [HttpPost]
        [Authorize]
        [Route("SetTemplateScripts")]
        public async Task<IActionResult> SetTemplateScripts(TemplateSettingsModel template)
        {
            try
            {
                template.UserId = await _workContext.GetCurrentUserId();
                return Ok(await _pageServices.SetTemplate(template, UpdateTemplateEnum.Scripts));
            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message );
            }

        }


        [HttpGet]
        [Authorize]
        [Route("GetTemplateScripts/{TemplateId}")]
        public async Task<IActionResult> GetTemplateScripts(long TemplateId)
        {
            try
            {
                var data = await _pageServices.GetTemplateData(TemplateId);
                return Ok(new { Script = data.Scripts });
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        [HttpPost]
        [Authorize]
        [Route("SetTemplateImportFiles")]
        public async Task<IActionResult> SetTemplateImportFiles(TemplateSettingsModel template)
        {
            try
            {
                template.UserId = await _workContext.GetCurrentUserId();

                return Ok(await _pageServices.SetTemplate(template, UpdateTemplateEnum.ImportFiles));
            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        [HttpGet]
        [Authorize]
        [Route("GetTemplateImportFiles/{TemplateId}")]
        public async Task<IActionResult> GetTemplateImportFiles(long TemplateId)
        {
            try
            {
                var data = await _pageServices.GetTemplateData(TemplateId);
                return Ok(new { ImportFilesList = data.ImportFilesList });
            }

            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }

        }

        [HttpGet]
        [Route("DeleteImportedFile/{ImportedFileId}")]
        [Authorize]
        public async Task<IActionResult> DeleteImportedFile(long ImportedFileId)
        {
            try
            {
                //var siteId = int.Parse(Request.Headers["siteId"]);
                var result = await _pageServices.DeleteImportedFile(ImportedFileId);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }
        #endregion
    }
}