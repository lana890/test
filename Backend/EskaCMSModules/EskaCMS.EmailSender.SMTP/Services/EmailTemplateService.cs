using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Models;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Core.Extensions;
using System.Drawing;

namespace EskaCMS.EmailSender.SMTP.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {

        private readonly IRepositoryWithTypedId<EmailTemplates, string> _emailtemplatesRepository;

        private IRepository<EmailTemplateType> _emailTemplateType;
        private IWorkContext _workContext;

        public EmailTemplateService( IRepository<EmailTemplateType> emailTemplateType,
                                     IRepositoryWithTypedId<EmailTemplates,string> emailtemplatesRepository,
                                     IWorkContext workContext)
        {
            _emailtemplatesRepository = emailtemplatesRepository;
            _emailTemplateType = emailTemplateType;
            _workContext = workContext;
        }
        public async Task<bool> AddTemplate(EmailTemplatesViewModel model)
        {
            try
            {
                var emailTemplate = new EmailTemplates(model.Id)
                {

                    Status = EStatus.Active,
                    CreationDate = DateTimeOffset.Now,
                    Id = model.Subject,
                    Color = model.Color,
                    Icon = model.Icon,
                    StatusDate = DateTimeOffset.Now,
                    AdditionalInfo = model.AdditionalInfo,
                    EmailBody = model.EmailBody,
                    EmailTemplateTypeId = model.EmailTemplateTypeId,
                    SiteId = _workContext.GetCurrentSiteId(),
                    IsHTML = model.IsHTML,
                    CultureId = model.CultureId,
                    CreatedById = await _workContext.GetCurrentUserId(),
                    Subject = model.Subject

                };
               

                _emailtemplatesRepository.Add(emailTemplate);
                await _emailtemplatesRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception exc)
            {
                
                throw ExceptionHelper.ThrowDeepException(exc);
            }
        }
        public async Task<bool> EditTemplate(EmailTemplatesViewModel model)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var emailtemplate =await _emailtemplatesRepository.Query().Where(x => x.Id == model.Id && siteId==x.SiteId).FirstOrDefaultAsync();
                emailtemplate.ModificationDate = DateTimeOffset.Now;
                emailtemplate.Status = EStatus.Active;
                emailtemplate.Color = model.Color;
                emailtemplate.ModifiedById = await _workContext.GetCurrentUserId();
                emailtemplate.Icon = model.Icon;
                emailtemplate.StatusDate = DateTimeOffset.Now;
                emailtemplate.AdditionalInfo = model.AdditionalInfo;
                emailtemplate.EmailBody = model.EmailBody;
                emailtemplate.EmailTemplateTypeId = model.EmailTemplateTypeId;
                _emailtemplatesRepository.Update(emailtemplate);
                await _emailtemplatesRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception exc)
            {

                throw ExceptionHelper.ThrowDeepException(exc);
            }
        }
        public async Task<bool> DeleteTemplate(string TemplateId, long UserId, long SiteId)
        {
            try
            {
                var template = await _emailtemplatesRepository.Query()
                    .Where(x => x.Id == TemplateId && x.SiteId == SiteId)
                    .FirstOrDefaultAsync();

                template.Status = EStatus.Deleted;
                template.ModifiedById = UserId;

                _emailtemplatesRepository.Update(template);
                await _emailtemplatesRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
        public async Task<EmailTemplateVM> GetEmailTemplateById(string Id)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var template = await _emailtemplatesRepository.Query().Where(x => x.Id == Id && (siteId == 0 ? true : x.SiteId == siteId)).Select(x => new EmailTemplateVM
                {
                    Id = x.Id,
                    CultureId = x.CultureId,
                    EmailTemplateTypeId = x.EmailTemplateTypeId,
                    IsHTML = x.IsHTML,
                    SiteId = x.SiteId,
                    Subject = x.Subject,
                    EmailBody = x.EmailBody,
                    AdditionalInfo = x.AdditionalInfo,
                    Color=x.Color,
                    Icon=x.Icon


                }).AsNoTracking().FirstOrDefaultAsync();
                return template;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
        public async Task<EmailTemplateListOutputVM<EmailTemplateOutputVM>> GetTemplates(EmailTemplateSearchVM search)
        {
            try
            {
                List<EmailTemplates> query = await _emailtemplatesRepository.Query().Where(x => x.Status == EStatus.Active && x.SiteId == search.SiteId)
                    .Include(x => x.EmailTemplateType)
                    .ToListAsync();
                if (search.EmailTemplateSort != null)
                {
                    query = SortEmailTemplates(search.EmailTemplateSort, query);
                }
                else
                {
                    query = query.OrderByDescending(x => x.Id).ToList();
                }
                List<EmailTemplates> EmailTemplateQuery = query
                                      .Where(email =>
                                          string.IsNullOrEmpty(search.SearchValue)
                                          || email.Subject.ToUpper().Contains(search.SearchValue.ToUpper()) || email.Id.ToString().ToUpper().Contains(search.SearchValue.ToUpper())
                                          || email.Id.ToUpper().Contains(search.SearchValue.ToUpper()) || email.Id.ToString().ToUpper().Contains(search.SearchValue.ToUpper())
                                          || email.EmailTemplateType.Name.ToUpper().Contains(search.SearchValue.ToUpper()))
                                      .ToList();

if( search.type != EmailType.All)
				{
					EmailTemplateQuery= EmailTemplateQuery.Where(x=> x.EmailTemplateTypeId == (int)search.type).ToList();
}
if(  !string.IsNullOrEmpty(search.cultureId) ){
					EmailTemplateQuery = EmailTemplateQuery.Where(x => x.CultureId == search.cultureId).ToList();
				}
                int totalCount = EmailTemplateQuery.Count;
                List<EmailTemplateOutputVM> outputList = EmailTemplateQuery.Select(email => new EmailTemplateOutputVM
                {
                    Id = email.Id,
                    Subject = email.Subject,
                    EmailType = email.EmailTemplateType.Name,
                    CultureId = email.CultureId

                }).Skip(search.PageNo * search.PageSize).Take(search.PageSize).ToList();

                return new EmailTemplateListOutputVM<EmailTemplateOutputVM>
                {
                    List = outputList.ToList(),
                    TotalCount = totalCount
                };

            }
            catch (Exception exc)
            {

                throw exc;
            }


        }
        private List<EmailTemplates> SortEmailTemplates(EmailTemplateSearchSortVM search, List<EmailTemplates> query)
        {
            if (search.SortField == EmailTemplateSearchSortFieldsEnum.Id)
            {
                if (search.SortType == EmailTemplateSearchSortTypeEnum.asc)
                {
                    query = query.OrderBy(x => x.Id).ToList();
                }
                else
                {
                    query = query.OrderByDescending(x => x.Id).ToList();
                }
            }


			if (search.SortField == EmailTemplateSearchSortFieldsEnum.Date)
			{
				if (search.SortType == EmailTemplateSearchSortTypeEnum.asc)
				{
					query = query.OrderBy(x => x.ModificationDate).ToList();
				}
				else
				{
					query = query.OrderByDescending(x => x.ModificationDate).ToList();
				}
			}



			if (search.SortField == EmailTemplateSearchSortFieldsEnum.Subject)
            {
                if (search.SortType == EmailTemplateSearchSortTypeEnum.asc)
                {
                    query = query.OrderBy(x => x.Subject).ToList();
                }
                else
                {
                    query = query.OrderByDescending(x => x.Subject).ToList();
                }
            }
            if (search.SortField == EmailTemplateSearchSortFieldsEnum.EmailType)
            {
                if (search.SortType == EmailTemplateSearchSortTypeEnum.asc)
                {
                    query = query.OrderBy(x => x.EmailTemplateType.Name).ToList();
                }
                else
                {
                    query = query.OrderByDescending(x => x.EmailTemplateType.Name).ToList();
                }
            }

            return query;

        }
        public async Task<List<EmailTemplateTypesVM>> GetTemplateTypes()
        {
            try
            {
                var types = await _emailTemplateType.Query().Select(x => new EmailTemplateTypesVM
                {
                    Id = x.Id,
                    Name = x.Name

                }).ToListAsync();
                return types;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<List<EmailTemplateVM>> GetEmailTemplateByTypeId(int typeId, long SiteId)
        {
            try
            {
                var template = await _emailtemplatesRepository.Query().Where(x => x.EmailTemplateTypeId == typeId && x.SiteId == SiteId && x.Status == EStatus.Active).Select(x => new EmailTemplateVM
                {
                    Id = x.Id,
                    CultureId = x.CultureId,
                    EmailTemplateTypeId = x.EmailTemplateTypeId,
                    EmailTemplateTypes = new EmailTemplateTypesVM() { Name = x.EmailTemplateType.Name },
                    IsHTML = x.IsHTML,
                    SiteId = x.SiteId,
                    Subject = x.Subject,
                    EmailBody = x.EmailBody,
                    Color = x.Color,    
                    Icon = x.Icon,  

                }
                ).ToListAsync();
                return template;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

    }
}
