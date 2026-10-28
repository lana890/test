using EskaCMS.Core.Extensions;
using EskaCMS.DynamicForms.Services;
using EskaCMS.DynamicForms.Services.IServices;
using EskaCMS.DynamicForms.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.DynamicForms.Controllers
{
    [Route("api/Pricing")]
    [ApiController]
    [Authorize]
    public class PricingContoller : ControllerBase
    {
        public IDynamicFormsService _DynamicFormsService { get; }
        public IFormItemService _FormItemService { get; }
        public IWorkContext _WorkContext { get; }
        private readonly IFormItemServiceNew _FormItemServiceNew;

        public PricingContoller(IDynamicFormsService DynamicFormsService,
            IFormItemService FormItemService, IWorkContext workContext,
            IFormItemServiceNew FormItemServiceNew)
        {
            _DynamicFormsService = DynamicFormsService;
            _FormItemService = FormItemService;
            _WorkContext = workContext;
            _FormItemServiceNew = FormItemServiceNew;
        }

        [HttpPost]
        [Route("Submit/{FormItemId}/{PageId}/{ComponentSettingsId}/{Culture}")]
        public async Task<IActionResult> Submitted(long FormItemId, long PageId, string ComponentSettingsId, string Culture)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());

                var UserId = await _WorkContext.GetCurrentUserId();

                var FormData = Request.Form["data"][Request.Form["data"].Count - 1];
                var pdata = Request.Form["pdata"][Request.Form["pdata"].Count - 1];
                var Files = Request.Form.Files;
                DynamicFormsSettingsVM? settingsData = null;

                if (Request.Form.TryGetValue("settings", out var settingsValues))
                {
                    var settings = settingsValues.LastOrDefault();

                    if (!string.IsNullOrWhiteSpace(settings) &&
                        !string.Equals(settings, "null", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            settingsData =
                                JsonConvert.DeserializeObject<DynamicFormsSettingsVM>(settings);
                        }
                        catch (JsonException ex)
                        {
                            return BadRequest(new
                            {
                                message = "The settings JSON is invalid.",
                                error = ex.Message
                            });
                        }
                    }
                }
                var PricingData = JsonConvert.DeserializeObject<List<DynamicFormsWebVM>>(pdata);
                var Result = new { result = await _DynamicFormsService.Submitted(FormData, settingsData, PricingData, FormItemId, SiteId, UserId, PageId, ComponentSettingsId, Culture, Files) };

                if (string.IsNullOrEmpty(Result.result))
                {
                    return Ok();
                }
                else
                {

                    return Ok(Result);
                }

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Route("GetAllFormItemsForWeb/{cultureId}/{FormItemId}")]
        [Route("GetAllFormItemsForWeb/{cultureId}")]
        public async Task<IActionResult> GetAllFormItemsForWeb([FromRoute] long cultureId, [FromBody] RequestFormItemWebViewModel model)
        {
            try
            {
                var SiteId = await _WorkContext.GetCurrentSiteIdAsync();


                return Ok(await _FormItemService.GetAllFormItemsForWeb(SiteId, cultureId, model));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }



        [HttpGet]
        [Route("GetAllFormItemsWithClassification/{cultureId}")]
        public async Task<IActionResult> GetAllFormItemsWithClassification([FromRoute] long cultureId)
        {
            try
            {
                var SiteId = await _WorkContext.GetCurrentSiteIdAsync();


                return Ok(await _FormItemService.GetAllFormItemsWithClassification(SiteId, cultureId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        [HttpGet]
        [Route("GetAllFormItemsWithClassificationPagination/{cultureId}")]
        [Route("GetAllFormItemsWithClassificationPagination")]
        public async Task<IActionResult> GetAllFormItemsWithClassificationPagination(
            string? SearchText,
            int? page = null, int? pageSize = null)
        {
            try
            {
                var SiteId = await _WorkContext.GetCurrentSiteIdAsync();


                return Ok(await _FormItemServiceNew.GetAllFormItemsWithClassificationPagination(SiteId, SearchText, page,
                    pageSize));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        [HttpGet]
        [Route("GetFormFieldByFormItemIdPagintaion/{cultureId}")]
        public async Task<IActionResult> GetFormFieldByFormItemIdPagintaion([FromRoute] long cultureId, long FormItemId,
           int? Page = null, int? PageSize = null)
        {
            try
            {
                // var SiteId = await _WorkContext.GetCurrentSiteIdAsync();
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());

                return Ok(await _FormItemServiceNew.GetFormFieldByFormItemIdPagintaion(cultureId, FormItemId, SiteId, Page, PageSize));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpGet]
        [Route("GetClassificationsByFormItemIdPagintaion/{cultureId}")]
        public async Task<IActionResult> GetClassificationsByFormItemIdPagintaion(long FormItemId, string? SearchText,
         int? Page = null, int? PageSize = null)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());

                return Ok(await _FormItemServiceNew.GetClassificationsByFormItemIdPagintaion(FormItemId, SiteId, SearchText, Page, PageSize));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

    }
}
