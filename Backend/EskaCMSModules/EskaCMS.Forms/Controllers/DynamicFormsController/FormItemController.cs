using EskaCMS.Core.Extensions;
using EskaCMS.DynamicForms.Services.IServices;
using EskaCMS.DynamicForms.ViewModels;
using EskaCMS.Media.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.DynamicForms.Controllers
{
    [Route("api/FormItem")]
    [ApiController]
    [Authorize]
    public class FormItemController : ControllerBase
    {

        private readonly IFormItemService _FormItemService;
        private readonly IWorkContext workContext;

        public FormItemController(IFormItemService FormItemService, IWorkContext workContext)
        {
            _FormItemService = FormItemService;
            this.workContext = workContext;
        }
        [HttpGet]
        [Route("GetFormItemsById/{FormItemId}")]
        public async Task<IActionResult> GetFormItemsById(long FormItemId)
        {
            try
            {
                return Ok(await _FormItemService.GetFormItemsById(FormItemId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpGet]
        [Route("GetAllFormItems")]
        public async Task<IActionResult> GetAllFormItems()
        {
            try
            {

                var SiteId = await workContext.GetCurrentSiteIdAsync();

                return Ok(await _FormItemService.GetAllFormItems(SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Route("UpsertFormItems")]
        public async Task<IActionResult> UpsertFormItems(UpsertFormItemVM Model)
        {
            try
            {
                var UserId = await workContext.GetCurrentUserId();
                var SiteId = await workContext.GetCurrentSiteIdAsync();

                return Ok(await _FormItemService.UpsertFormItems(Model, UserId, SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Route("UpdateFormItemsAdditionalInfo")]
        public async Task<IActionResult> UpdateFormItemsAdditionalInfo([FromBody] UpdateAdditionalInfoVM model)
        {
            try
            {
                var result = await _FormItemService.UpdateFormItemsAdditionalInfo(model.Id, model.AdditionalInfo);

                return Ok(result);

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Route("AssignFormItemPermissions")]
        public async Task<IActionResult> AssignFormItemPermissions([FromBody] RequestFormItemRoleViewModel model)
        {
            try
            {
                await _FormItemService.AssignFormItemPermissions(model);
                return Ok();

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("AssignedFormItemRoles/{Id}")]

        public async Task<IActionResult> AssignedFormItemRoles([FromRoute] long Id)
        {
            try
            {

                return Ok(await _FormItemService.AssignedFormItemRoles(Id));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpDelete]
        [Route("DeleteFormItems/{FormItemId}")]
        public async Task<IActionResult> DeleteFormItems(long FormItemId)
        {
            try
            {
                var UserId = await workContext.GetCurrentUserId();

                return Ok(await _FormItemService.UpdateFormItemsStatus(FormItemId, EStatus.Deleted, UserId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

    }
}