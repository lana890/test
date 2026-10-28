using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.EmailSender.SMTP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Forms.Services.FormsService.Interfaces;
using EskaCMS.Forms.Model.FormsViewModel;

namespace EskaCMS.Forms.Controllers.FormsController
{
    [Route("api/Forms")]
    [ApiController]
    [Authorize]
    public class FormController : ControllerBase
    {
        private readonly IFormService _FormServices;
        private readonly ISubmittedFormService _submittedFormServices;


        public FormController(IFormService FormServices , ISubmittedFormService submittedFormServices)
        {
            _FormServices = FormServices;
            _submittedFormServices=submittedFormServices;

        }

        [HttpPost]
        [Route("SaveForm/{PageId}/{ComponentSettingsId}/{Culture}")]
        public async Task<IActionResult> SaveForm(long PageId, string ComponentSettingsId, string Culture)
        {
            try
            {


                var data = Request.Form["data"][Request.Form["data"].Count - 1];
                var Files = Request.Form.Files;
                var HostName = Request.Form["hostName"][Request.Form["hostName"].Count - 1];
                await _FormServices.NewFormSave(PageId, ComponentSettingsId, Culture, data, Files, HostName);
                return Ok(true);


            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    return BadRequest(ex.InnerException.ToString());
                }
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("getSubmittedForms")]
        public ActionResult GetSubmittedForms([FromBody] SearchTableParam<FormDataViewModel> param)
        {
            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());

                return Ok(_FormServices.GetSubmittedForms(SiteId, param));
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    return BadRequest(ex.InnerException.ToString());
                }
                return BadRequest(ex.Message);
            }
            
        }
        [HttpPost]
        [Route("UpsertFormBuilderSettings")]
        public async Task<IActionResult> UpsertFormBuilderSettings([FromBody] List<FormBuilderVm> formBuilder)
        {
            try
            {
                return Ok(await _FormServices.UpsertFormBuilderSettings(formBuilder));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }


        [HttpGet]
        [Route("GetAllForms/{PageId}")]
        public async Task<IActionResult> GetAllForms([FromRoute]long PageId)
        {
            try
            {
               ;
                return Ok(await _FormServices.GetAllForms(PageId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
        [HttpGet]
        [Route("GetAllKeys")]
        public async Task<IActionResult> GetAllKeys([FromQuery] long PageId)
        {
            try
            {
                return Ok(await _FormServices.GetAllKeys(PageId));


            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
        [HttpGet]
        [Route("GetFormEdit")]
        public async Task<IActionResult> GetFormEdit([FromQuery]long Id)
        {
            try
            {
                return Ok(await _FormServices.GetFormEdit(Id));


            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
        
        [HttpPost]
        [Route("WorkFlow/SubmitForm")]

        public async Task<IActionResult> SubmitForm([FromForm] SubmittedFormVM model)
        {
            try
            {
                var result = await _submittedFormServices.SubmitForm(model);

               
                return Ok(result);



            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("WorkFlow/GetSubmittedForms")]

        public async Task<IActionResult> GetAllSubmittedForm([FromBody] SmartTableParam model)
        {
            try
            {
                var result = await _submittedFormServices.GetAllSubmittedForm(model);

                return Ok(result);


            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("WorkFlow/GetSubmittedFormByCode")]

        public async Task<IActionResult> GetSubmittedFormByCode([FromBody] SubmittedFormByCodeVM model)
        {
            try
            {
                var result = await _submittedFormServices.GetSubmittedFormByCode(model);

                return Ok(result);


            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("WorkFlow/GetAllSubmittedForms")]
        public async Task<IActionResult> GetSubmittedForms([FromBody] SubmittedFormByCodeVM model)
        {
            try
            {
                var result = await _submittedFormServices.AllSubmittedByStatusOrCode(model);

                return Ok(result);


            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost] 
        [Route("WorkFlow/GetStatistics")]
        public async Task<IActionResult> GetStatistics([FromBody] StatisticsVM model)
        {
            try
            {
                var result = await _submittedFormServices.GetStatistics(model);

                return Ok(result);


            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

       

    }
}
