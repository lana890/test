using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EskaCMS.BLL.Services;
using EskaCMS.BLL.Services.Interfaces;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static BusinessModels.BusinessModel;
using EskaCMS.Menus.Models;
using EskaCMS.EmailSender.SMTP.Services;
using Microsoft.AspNetCore.Authorization;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;
using System.Web;
using EskaCMS.General.ViewModels;

namespace EskaCMS.APIs.Controllers
{
    [Route("api/General")]
    [ApiController]
    [Authorize]
    public class GeneralController : ControllerBase
    {

        private readonly IGeneral _GeneralServices;
        // private readonly IEmailSender _emailSender;
        private readonly IWorkContext _workContext;
        private readonly ISiteSettingsService _SiteSettingsService;
        public const string MatchEmailPattern =
          @"^(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@"
          + @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\."
            + @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
          + @"([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,4})$";

        public static bool IsEmail(string email)
        {
            if (email != null) return Regex.IsMatch(email, MatchEmailPattern);
            else return false;
        }
        public GeneralController(IGeneral GeneralServices, /* IEmailSender emailSender, */ IWorkContext workContext, ISiteSettingsService siteSettingsService)
        {
            // _BuildVersionsRepository = BuildVersionsRepository;
            _GeneralServices = GeneralServices;
            // _emailSender = emailSender;
            _workContext = workContext;
            _SiteSettingsService = siteSettingsService;
            // _AppKeys = Configuration["AppSettings:CMSConfig"];
            // _BundleDestination = Configuration.GetSection("CMSConfig").GetSection("BuilderDestination").Value;
            // _WebDestination = Configuration.GetSection("CMSConfig").GetSection("WebDestination").Value;
        }


        [HttpGet]
        [Route("GetSiteSlugs")]
        public async Task<IActionResult> GetSiteSlugs()
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                return Ok(await _GeneralServices.GetSiteSlugs(SiteId, await _workContext.GetCurrentUserId()));
            }
            catch (Exception exc)
            {

                return BadRequest(exc);
            }
        }

        [HttpGet]
        [Route("CheckSiteSlug/{slug}")]
        public async Task<IActionResult> CheckSiteSlugs(string slug)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();

                return Ok(await _GeneralServices.CheckSiteSlugAsync(SiteId, slug));
            }
            catch (Exception exc)
            {

                return BadRequest(exc);
            }
        }



        [HttpGet]
        [Route("GetAllVersion")]
        public IActionResult GetAllVersion()
        {
            try
            {
                return Ok(_GeneralServices.GetAllVersion());
            }
            catch (Exception exc)
            {

                return BadRequest(exc);
            }
        }

        [HttpGet]
        [Route("IsLatestVersion/{CurrentVersion}")]
        public async Task<IActionResult> IsLatestVersion(string CurrentVersion)
        {
            try
            {
                string latestVersion = await _SiteSettingsService.GetReleaseVersion();
               
                return Ok(latestVersion.ToLower() == HttpUtility.UrlDecode(CurrentVersion).ToLower());
            }
            catch (Exception exc)
            {

                return BadRequest(exc);
            }
        }


        [HttpGet]
        [Route("GetSiteSettings/{Id}")]
        public async Task<IActionResult> GetSiteSettings(string Id)
        {
            try
            {
           

                return Ok(await _SiteSettingsService.GetSiteSettingById(Id,_workContext.GetCurrentSiteId()));
            }
            catch (Exception exc)
            {

                return BadRequest(exc);
            }
        }


        [HttpGet]
        [Route("GetAllCulture")]
        public IActionResult GetAllCulture()
        {
            try
            {
                return Ok(_GeneralServices.GetAllCulture());
            }
            catch (Exception exc)
            {

                return BadRequest(exc);
            }
        }


        [HttpGet]
        [RouteAttribute("GetCultureBySiteId")]
        public async Task<IActionResult> GetCulturebysiteId()
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();

                return Ok(await _GeneralServices.GetCulturebysiteId(SiteId));
            }
            catch (Exception exc)
            {

                return BadRequest(exc);
            }
        }
        [HttpPost]
        [Route("TransferCMSFiles")]
        public async Task<IActionResult> TransferCMSFiles()
        {
            try
            {
                var FileName = Request.Form["FileName"][Request.Form["FileName"].Count - 1];
                var Description = Request.Form["Description"][Request.Form["Description"].Count - 1];
                var CreatedById = await _workContext.GetCurrentUserId();
                var SiteId = _workContext.GetCurrentSiteId();
                var ComponentType = Request.Form["ComponentType"][Request.Form["ComponentType"].Count - 1];
                var Files = Request.Form.Files;
                var objComponentType = int.Parse(ComponentType);

                return Ok(await _GeneralServices.TransferCMSFiles(FileName, Description, CreatedById, SiteId, (EComponentType)objComponentType, Files));
            }
            catch (Exception exc)
            {

                return BadRequest(exc.Message);
            }
        }

        //[HttpPost]
        //[Route("AddNewTypesSettings")]
        //public async Task<IActionResult> AddNewTypesSettings(TypesSettingsViewModel TypesSettings)
        //{
        //    try
        //    {
        //        return Ok(await _GeneralServices.AddNewTypesSettings(TypesSettings));
        //    }
        //    catch (Exception exc)
        //    {
        //        return BadRequest(exc.Message);
        //    }
        //}

        //[HttpPost]
        //[Route("EditTypesSettings/{TypesSettingsId}")]
        //public async Task<IActionResult> EditTypesSettings(long TypesSettingsId, TypesSettingsViewModel TypesSettings)
        //{
        //    try
        //    {
        //        return Ok(await _GeneralServices.EditTypesSettings(TypesSettingsId, TypesSettings));
        //    }
        //    catch (Exception exc)
        //    {
        //        return BadRequest(exc.Message);
        //    }
        //}


        //[HttpGet]
        //[Route("GetTypesSettingsByTypesSettingsId/{TypesSettingsId}")]
        //public async Task<IActionResult> GetTypesSettingsByTypesSettingsId(long TypesSettingsId)
        //{
        //    try
        //    {
        //        return Ok(await _GeneralServices.GetTypesSettingsByTypesSettingsId(TypesSettingsId));
        //    }
        //    catch (Exception exc)
        //    {

        //        return BadRequest(exc);
        //    }
        //}
        //[HttpGet]
        //[Route("GetTypesSettingsByPostId/{PostId}")]
        //public async Task<IActionResult> GetTypesSettingsByPostId(long PostId)
        //{
        //    try
        //    {
        //        return Ok(await _GeneralServices.GetTypesSettingsByPostId(PostId));
        //    }
        //    catch (Exception exc)
        //    {

        //        return BadRequest(exc);
        //    }
        //}
        //[HttpDelete]
        //[Route("DeleteTypesSettings/{TypesSettingsId}")]
        //public async Task<IActionResult> DeleteTypesSettings(long TypesSettingsId)
        //{
        //    try
        //    {
        //        return Ok(await _GeneralServices.DeleteTypesSettings(TypesSettingsId));

        //    }
        //    catch (Exception exc)
        //    {

        //        return BadRequest(exc);
        //    }
        //}




        [HttpGet]
        [Route("GetBreadcrumByType/{type}/{id}")]
        public async Task<IActionResult> GetBreadcrumBySlug(int type, long id)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                return Ok(await _GeneralServices.GetBreadcrumByType(type, id, siteId));

            }
            catch (Exception exc)
            {

                return BadRequest(exc);
            }
        }


        [HttpPost]
        [Route("MiladyHijriConverter")]
        public IActionResult MiladyHijriConverter([FromBody] DateConverterVM payLoad)
        {
            try
            {
                if (payLoad.IsConvertHijriToMilady)
                {
                    return Ok(_GeneralServices.HijriToGreg(payLoad.DateValue));
                }
                else
                {
                    return Ok(_GeneralServices.GregToHijri(payLoad.DateValue));
                }
            }
            catch (Exception exc)
            {

                return BadRequest(exc);
            }
        }


        [HttpGet]
        [Route("GetPageAssignedRoles/{id}")]
        public async Task<IActionResult> GetMenuAssignedRoles(long id)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var result = await _GeneralServices.GetPageAssginedRoles(id, SiteId);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }

        [HttpPost]
        [Route("SavePageAssginedRoles")]
        public async Task<IActionResult> SaveMenuAssginedRoles(MenuRolesViewModel vm)
        {
            try
            {
                vm.UserId =await _workContext.GetCurrentUserId();
                var result = await _GeneralServices.SavePageAssginedRoles(vm);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }




    }
}