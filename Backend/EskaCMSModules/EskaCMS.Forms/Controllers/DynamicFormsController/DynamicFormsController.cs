
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using EskaCMS.DynamicForms.Services.IServices;
using EskaCMS.DynamicForms.ViewModels;
using System.Collections.Generic;
using EskaCMS.Core.Extensions;
using Newtonsoft.Json;
using EskaCMS.Forms.Model;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.APIs.Controllers
{
    [Route("api/DynamicForms")]
    [ApiController]
    [Authorize]
    public class DynamicFormsController : ControllerBase
    {

        private readonly IDynamicFormsService _DynamicFormsService;
        private readonly IWorkContext _WorkContext;

        public DynamicFormsController(IDynamicFormsService DynamicFormsService, IWorkContext WorkContext)
        {
            _DynamicFormsService = DynamicFormsService;
            _WorkContext = WorkContext;
        }


        [HttpGet]
        [Route("GetClassificationById/{Id}")]
        public async Task<IActionResult> GetClassificationById(long Id)
        {
            try
            {
                return Ok(await _DynamicFormsService.GetClassificationById(Id));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpGet]
        [Route("GetClassifications/{FormItemId}")]
        public async Task<IActionResult> GetClassifications(long FormItemId)
        {
            try
            {
                return Ok(await _DynamicFormsService.GetClassifications(FormItemId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpDelete]
        [Route("DeleteClassification/{Id}")]
        public async Task<IActionResult> DeleteClassification(long Id)
        {
            try
            {
                var UserId = await _WorkContext.GetCurrentUserId();

                return Ok(await _DynamicFormsService.UpdateClassificationStatus(Id, EStatus.Deleted, UserId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpGet]
        [Route("DuplicateFormItem/{FormItemId}")]
        public async Task<IActionResult> DuplicateFormItem(long FormItemId)
        {
            try
            {
                return Ok(new { result = await _DynamicFormsService.DuplicateFormItem(FormItemId) });

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


    }
}