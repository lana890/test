using EskaCMS.Entities.Entity;
using EskaCMS.Core.Models;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.EmailSender.SMTP.Services
{
    public interface IEmailTemplateService
    {
        Task<bool> AddTemplate(EmailTemplatesViewModel model);
        Task<bool> EditTemplate(EmailTemplatesViewModel model);
        Task<bool> DeleteTemplate(string TemplateId,long UserId, long SiteId);
        Task<EmailTemplateVM> GetEmailTemplateById(string TemplateId);

        Task<EmailTemplateListOutputVM<EmailTemplateOutputVM>> GetTemplates(EmailTemplateSearchVM search);

         Task<List<EmailTemplateTypesVM>> GetTemplateTypes();
        Task<List<EmailTemplateVM>> GetEmailTemplateByTypeId(int TypeId, long SiteId);
    }
}
