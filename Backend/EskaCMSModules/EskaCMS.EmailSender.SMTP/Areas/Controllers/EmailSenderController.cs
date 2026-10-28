using EskaCMS.Entities.Entity;
using EskaCMS.Core.Services;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using EskaCMS.EmailSender.SMTP.Services;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using EskaCMS.Core.Areas.Core.ViewModels.AppSettingsModels;
using EskaCMS.Core.Extensions;

namespace EskaCMS.EmailSender.SMTP.Areas.Controllers
{
    [ApiController]
    [Route("api/EmailSender")]
    [Authorize]
    public class EmailSenderController : ControllerBase
    {
        private readonly IEmailSender _emailSender;
        public readonly IWorkContext _workContext;
        // private readonly IEmailTemplateService _emailTemplateService;
        public EmailSenderController(IEmailSender emailSender,
            /* IEmailTemplateService emailTemplateService, */
            IWorkContext workContext)
        {
            _emailSender = emailSender;
            _workContext = workContext;
            // _emailTemplateService = emailTemplateService;
        }

        //[HttpPost("sendEmailByTemplateId")]
        //public async Task<IActionResult> SendEmail([FromBody] EmailSenderVM model)
        //{
        //    try
        //    {
        //        // var emailTemplateObj = await _emailTemplate.Query().Where(x => x.Id == model.TemplateId).FirstOrDefaultAsync();
        //        var emailTemplateObj = await _emailTemplateService.GetEmailTemplateById(model.TemplateId);
        //        if (model.Params.Count() > 0)
        //        {
        //            foreach (var item in model.Params)
        //            {
        //                emailTemplateObj.EmailBody = emailTemplateObj.EmailBody.Replace(item.Param, item.Value);
        //            }
        //        }

        //        await _emailSender.SendEmailAsync(model.Emails, emailTemplateObj.Subject,emailTemplateObj.EmailBody, emailTemplateObj.SiteId, emailTemplateObj.IsHTML, model.Attachment);
        //        return Ok();
        //    }
        //    catch (Exception exc)
        //    {

        //        throw exc;
        //    }
        //}

        [HttpPost]
        [Route("TestEmailSettings")]
        public async Task<IActionResult> TestEmailSettings([FromBody] TestEmailSettingsVM testEmailSettingsVM)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                await _emailSender.TestEmailSettings(testEmailSettingsVM, SiteId);


                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
