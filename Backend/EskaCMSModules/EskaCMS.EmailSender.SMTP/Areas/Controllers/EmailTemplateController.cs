using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;

using EskaCMS.EmailSender.SMTP.Services;
using Microsoft.AspNetCore.Mvc;
using System;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using System.Web;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.EmailSender.SMTP.Areas.Controllers
{
    [ApiController]
    [Route("api/EmailTemplate")]
    [Authorize]
    public class EmailTemplateController : ControllerBase
    {
        private readonly IEmailTemplateService _emailTemplate;
        private readonly IWorkContext _workContext;

        public EmailTemplateController(IEmailTemplateService emailTemplate, IWorkContext workContext)
        {
            _emailTemplate = emailTemplate;
            _workContext = workContext;
        }


        [HttpPost("addTemplate")]
        public async Task<IActionResult> AddTemplate([FromBody] EmailTemplatesViewModel model)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                model.SiteId = SiteId;
                model.IsHTML = true;
                await _emailTemplate.AddTemplate(model);
                return Ok();
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message.Contains("Duplicate entry")?"Subject already used": exc.Message);
            }
        }

        

        [HttpPut("EditTemplate")]
        public async Task<IActionResult> EditTemplate([FromBody] EmailTemplatesViewModel model)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = _workContext.GetCurrentSiteId();                
                model.SiteId = SiteId;
                await _emailTemplate.EditTemplate(model);
                return Ok();
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message.Contains("Duplicate entry") ? "Subject already used" : exc.Message);
            }
        }
        


        [HttpPost("GetEmailTemplateById")]
        public async Task<IActionResult> GetEmailTemplateById([FromBody] GetEmailTemplateByIdVM Model)
        {
            try
            {
                var template = await _emailTemplate.GetEmailTemplateById(Model.Id);
                return Ok(template);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }


        [HttpDelete("DeleteTemplate/{TemplateId}")]
        public async Task<IActionResult> DeleteTemplate([FromRoute]string TemplateId)
        {
            try
            {
                var r = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnoreCase);
                TemplateId= r.Replace(TemplateId.ToString(), " ");

                var UserId = await _workContext.GetCurrentUserId();
                var SiteId = Convert.ToInt64(Request.Headers["siteId"]);
                 return Ok(await _emailTemplate.DeleteTemplate(TemplateId, UserId, SiteId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost("GetAllEmailTemplate")]
        public async Task<IActionResult> GetAllEmailTemplate([FromBody] EmailTemplateSearchVM search)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                search.SiteId = SiteId;
                var templates = await _emailTemplate.GetTemplates(search);
                return Ok(templates);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }


        [HttpGet("GetEmailTemplateTypes")]
        public async Task<IActionResult> GetEmailTemplateTypes()
        {
            try
            {
                var EmailTypes = await _emailTemplate.GetTemplateTypes();
                return Ok(EmailTypes);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpGet("GetEmailTemplateByTypeId/{typeId}")]
        public async Task<IActionResult> GetEmailTemplateByTypeId([FromRoute]int typeId)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var EmailTypes = await _emailTemplate.GetEmailTemplateByTypeId(typeId, SiteId);
                return Ok(EmailTypes);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
    }
}

