using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EskaCMS.Core.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Theming.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using EskaCMS.Core.Extensions;
using EskaCMS.Theming.ThemeVM;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.APIs.Controllers
{
    
        [Route("api/Themes")]
        [ApiController]
        public class ThemeController : ControllerBase
        {

            private readonly ITheme _ThemeService;
            private readonly IWorkContext _workContext;

        public ThemeController(ITheme ThemeService, IWorkContext workContext)

        {
                _ThemeService = ThemeService;
                 _workContext = workContext;

        }




        [HttpGet]
        [Route("GetThemes")]
        [Authorize]
        public async Task<IActionResult> getThemes()
            {
                try
                {
                var SiteId = _workContext.GetCurrentSiteId();
                //var UserId = int.Parse(Request.Headers["UserId"]);

                return Ok(await _ThemeService.GetThemesAsync(SiteId));
                }
                catch (Exception exc)
                {
                    return BadRequest(exc);
                }
            }



        [HttpPost]
        [Route("SaveTheme/{Id}")]
        [Authorize]
        public async Task<IActionResult> SaveTheme(Theme vm, long Id)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var UserId = await _workContext.GetCurrentUserId();
                var result = await _ThemeService.SaveTheme(vm, Id, UserId, SiteId);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }
        [HttpPost]
        [Route("SaveThemeFonts/{Id}")]
        [Authorize]
        public async Task<IActionResult> SaveThemeFonts(ThemeVM theme, long Id)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var result = await _ThemeService.SaveThemefonts(theme.Fonts, Id, siteId);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }
        [HttpGet]
        [Route("GetThemeFonts/{Id}")]
        [Authorize]
        public async Task<IActionResult> GetThemeFonts(long Id)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                ThemeVM themefont = new ThemeVM();

                themefont.Fonts = await _ThemeService.GetThemeFonts(Id,siteId);
               
                return Ok(themefont.Fonts);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }
        [HttpGet]
        [Route("GetThemeColors/{Id}")]
        [Authorize]
        public async Task<IActionResult> GetThemeColors(long Id)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                ThemeVM themecolor = new ThemeVM();

                themecolor.Colors = await _ThemeService.GetThemeColors(Id,siteId);

                return Ok(themecolor.Colors);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }

        [HttpPost]
        [Route("SaveThemeColors/{Id}")]
        [Authorize]
        public async Task<IActionResult> SaveThemeColors(ThemeVM theme, long Id)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var result = await _ThemeService.SaveThemeColors(theme.Colors, Id,siteId);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }

        [HttpPost]
        [Route("SaveThemeClass/{Id}")]
        [Authorize]
        public async Task<IActionResult> SaveThemeClass(ThemeVM theme, long Id)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var result = await _ThemeService.SaveThemeClass(theme.Classes,Id,siteId);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }
        [HttpGet]
        [Route("GetThemeClass/{Id}")]
        [Authorize]
        public async Task<IActionResult> GetThemeClass(long Id)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();

                var result = await _ThemeService.GetThemeClass(Id,siteId);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }

        [HttpPost]
        [Route("SaveThemeStyle/{Id}")]
        [Authorize]
        public async Task<IActionResult> SaveThemeStyle(ThemeVM theme, long Id)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var result = await _ThemeService.SaveThemeStyle(theme.Styles, Id,siteId);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }
        [HttpGet]
        [Route("GetThemestyle/{Id}")]
        [Authorize]
        public async Task<IActionResult> GetThemestyle(long Id)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var result = await _ThemeService.GetThemestyle(Id, siteId);
                return Ok(new { themestyle = result });
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }
        [HttpPost]
        [Route("SaveThemeScript/{Id}")]
        [Authorize]
        public async Task<IActionResult> SaveThemeScript(ThemeVM theme, long Id)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var result = await _ThemeService.SaveThemeScript(theme.Scripts, Id,siteId);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }

        [HttpGet]
        [Route("GetThemeScript/{Id}")]
        [Authorize]
        public async Task<IActionResult> GetThemeScript(long Id)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var result = await _ThemeService.GetThemeScript(Id, siteId);
                return Ok(new { themeScript = result });
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
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
                var result = await _ThemeService.DeleteImportedFile(ImportedFileId);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }


        [HttpPost]
        [Route("SaveThemeFile/{Id}")]
        [Authorize]
        public async Task<IActionResult> SaveThemeFile(ThemeVM theme, long Id)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var result = await _ThemeService.SaveThemeFile(theme.ImportedFiles, Id,siteId);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }


        [HttpGet]
        [Route("GetThemefile/{Id}")]
        [Authorize]
        public async Task<IActionResult> GetThemefile(long Id)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();

                var result = await _ThemeService.GetThemefile(Id,siteId);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }


        [HttpPost]
        [Route("SetActiveTheme/{Id}")]
        [Authorize]
        public async Task<IActionResult> IsActive(long Id)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                long UserId = await _workContext.GetCurrentUserId();
                var result = await _ThemeService.SetActiveThemeAsync( Id, UserId,SiteId);
                    return Ok(result);
            }
            catch (Exception exc)
            {
            return BadRequest(exc);
            }
        }

        [HttpDelete]
        [Route("DeleteTheme/{Id}")]
        [Authorize]
        public async Task<IActionResult> DeleteTheme(long Id)
        {
            try
            {
                bool result = await _ThemeService.DeleteTheme(Id);
                return Ok(result);
            }
            catch(Exception ex)
            {
                return BadRequest(ex);
            }
        }



        }
    }
