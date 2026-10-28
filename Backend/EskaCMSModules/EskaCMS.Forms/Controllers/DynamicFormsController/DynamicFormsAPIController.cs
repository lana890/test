
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
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Forms.Model.FormsViewModel;
using System.Linq;

namespace EskaCMS.APIs.Controllers
{
    [Route("api/DynamicFormsAPI")]
    [ApiController]
    [Authorize]
    public class DynamicFormsAPIController : ControllerBase
    {

        private readonly IDynamicFormsService _DynamicFormsService;
        private readonly ISubmissionServices submissionServices;
        private readonly IWorkContext _workContext;

        public DynamicFormsAPIController(IDynamicFormsService DynamicFormsService,
            ISubmissionServices submissionServices,

            IWorkContext workContext
            )
        {
            _DynamicFormsService = DynamicFormsService;
            this.submissionServices = submissionServices;
            _workContext = workContext;
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
        [HttpPost]
        [Route("UpsertClassification")]
        public async Task<IActionResult> UpsertClassification(ClassificationVM Model)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = _workContext.GetCurrentSiteId(); ;

                return Ok(await _DynamicFormsService.UpsertClassification(Model, UserId, SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Route("UpsertOperationCriateria")]
        public async Task<IActionResult> UpsertOperationCriateria(CriteriaVM Model)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());

                return Ok(await _DynamicFormsService.UpsertOperationCriateria(Model, UserId, SiteId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Route("GetPaginatedClassifications/{FormItemId}")]
        public async Task<IActionResult> GetPaginatedClassifications([FromRoute] long FormItemId, [FromBody] SearchTableParam<string> Data)
        {
            try
            {
                return Ok(await _DynamicFormsService.GetPaginatedClassifications(FormItemId, Data));

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
                var UserId = await _workContext.GetCurrentUserId();

                return Ok(await _DynamicFormsService.UpdateClassificationStatus(Id, EStatus.Deleted, UserId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpDelete]
        [Route("Criteria/{Id}")]
        public async Task<IActionResult> DeleteCriteria(long Id)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();

                return Ok(await _DynamicFormsService.UpdateCriteriaStatus(Id, EStatus.Deleted, UserId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpGet]
        [Route("GetClassificationOperation/{ClassId}")]
        public async Task<IActionResult> GetClassificationOperation(long ClassId)
        {
            try
            {
                return Ok(await _DynamicFormsService.GetClassificationOperation(ClassId));

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Route("GetGeneralClassifications")]
        public async Task<IActionResult> GetGeneralClassifications([FromBody] SmartSearchTableParam<AllGeneralFormField> model)
        {
            try
            {
                var result = await _DynamicFormsService.GetGeneralClassifications(model);
                return Ok(result);

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        [HttpPost]
        [Route("GetGeneralCriteria")]
        public async Task<IActionResult> GetGeneralCriteria([FromBody] SmartSearchTableParam<AllGeneralFormField> model)
        {
            try
            {
                var result = await _DynamicFormsService.GetGeneralCriteria(model);
                return Ok(result);

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("UpsertGeneralCriteria")]
        public async Task<IActionResult> UpsertGeneralCriteria(CriteriaVM model)
        {
            try
            {
                var result = await _DynamicFormsService.UpsertGeneralCriteria(model);
                return Ok(result);

            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        //General Submitted Forms 

        [HttpPost]
        [Route("GetSubmission")]
        public async Task<IActionResult> GetSubmission(RequestSubmission model)
        {
            try
            {
                var result = await submissionServices.GetSubmission(model);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        [HttpPost]
        [Route("GetAllFormItemForAdmin")]
        public async Task<IActionResult> GetAllFormItemForAdmin([FromBody] FilterFormItemViewModel model)
        {
            try
            {
                var result = await _DynamicFormsService.GetAllFormItemForAdmin(model);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPost]
        [Route("ExportSubmission")]
        public async Task<IActionResult> ExportSubmission([FromBody] RequestSubmission model)
        {
            try
            {
                var result = await submissionServices.GetSubmission(model);

                var exportdata = result.SubmissionInfo.Data.Select(x => new ExportSubmissionVM
                {

                    CreationDate = x.CreationDate,
                    FormItemName = x.FormItemName,
                    FullName = x.FullName,
                    NumberOfQuestions_NumberOfAnswers = x.NumberOfFields + "/" + x.NumberOfFieldsAnswered,
                    Question_Answer = string.Join(", ",
                                                x.SubmissionInfo.Select(f => $"{f.FormFieldName} = {f.Value}"))
                }).ToList();

                var file = await ExcelHelper.ExportExcelFile(exportdata, "UsersSubmitted");
                return File(file.FileBytes, file.ContentType, file.Name);
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        [HttpGet]
        [Route("GetSubmissionStatistics/{FormItemId}")]
        public async Task<IActionResult> GetSubmissionStatistics([FromRoute] long FormItemId)
        {
            try
            {
                var result = await submissionServices.GetSubmissionStatistics(FormItemId);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
    }
}