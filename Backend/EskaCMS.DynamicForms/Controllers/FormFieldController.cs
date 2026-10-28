
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using EskaCMS.DynamicForms.Services.IServices;
using EskaCMS.DynamicForms.ViewModels;
using EskaCMS.Core.Extensions;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Areas.Core.ViewModels;

namespace EskaCMS.APIs.Controllers
{
    [Route("api/FormField")]
    [ApiController]
    //[Authorize]
    public class FormFieldController : ControllerBase
    {

        private readonly IFormFieldService _SpecificationService;
        private readonly IWorkContext _workContext;

        public FormFieldController(IFormFieldService SpecificationService, IWorkContext workContext)
        {
            _SpecificationService = SpecificationService;
            _workContext = workContext;
        }

        [HttpPost]
        [Route("UpsertGeneralFormField")]
        public async Task<IActionResult> UpsertGeneralFormField(FormFieldVMs Model)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());

                return Ok(await _SpecificationService.UpsertFormField(new List<FormFieldVMs> { Model }, UserId, SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpDelete]
        [Route("DeleteGroup/{GroupId}")]
        public async Task<IActionResult> DeleteGroup(long GroupId)
        {
            try
            {
                return Ok(await _SpecificationService.DeleteGroup(GroupId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpGet]
        [Route("GetFormFieldGroups/{FormItemId}")]
        public async Task<IActionResult> GetFormFieldGroups(long FormItemId)
        {
            try
            {
                return Ok(await _SpecificationService.GetFormFieldGroups(FormItemId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Route("UpsertGroup")]
        public async Task<IActionResult> UpsertGroup([FromBody] List<FormFieldGroupVM> Model)
        {
            try
            {
                return Ok(await _SpecificationService.UpsertGroup(Model));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpGet]
        [Route("GetFormFieldById/{Id}")]
        public async Task<IActionResult> GetFormFieldById(long Id)
        {
            try
            {
                return Ok(await _SpecificationService.GetFormFieldById(Id));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpGet]
        [Route("GetFormFields/{FormItemId}")]
        public async Task<IActionResult> GetFormFields(long FormItemId)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());

                return Ok(await _SpecificationService.GetFormFields(FormItemId, SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("GetGeneralFormFields")]
        public async Task<IActionResult> GetGeneralFormFields([FromBody] SmartSearchTableParam<AllGeneralFormField> model)
        {
            try
            {
                var result = await _SpecificationService.GetGeneralFormFields(model);

                return Ok(result);

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("UpsertFormField")]
        public async Task<IActionResult> UpsertFormField(List<FormFieldVMs> Model)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());

                return Ok(await _SpecificationService.UpsertFormField(Model, UserId, SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpGet]
        [Route("GetNumaricalFormFields/{FormItemId}")]
        public async Task<IActionResult> GetNumaricalSpecifications(long FormItemId)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());

                return Ok(await _SpecificationService.GetNumaricalFormFields(FormItemId, SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpDelete]
        [Route("DeleteFormField/{Id}")]
        public async Task<IActionResult> DeleteFormField(long Id)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();

                return Ok(await _SpecificationService.UpdateFormFieldStatus(Id, EStatus.Deleted, UserId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }



    }
}