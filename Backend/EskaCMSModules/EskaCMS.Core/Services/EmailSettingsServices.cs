using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels.AppSettingsModels;
using EskaCMS.Core.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using MimeKit.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using MailKit.Net.Smtp;

using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using System.Reflection;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.DataAccess.Data;

namespace EskaCMS.Core.Services
{
    public class EmailSettingsServices : IEmailSettingsServices
    {
        private readonly IdcmsLogService _dcmsLogService;
        private readonly string _assembly;
        private readonly IRepository<EmailsSettings> _EmailsSettingsRepo;
        private readonly IWorkContext _workContext;
        private readonly EskaDCMSDbContext _context;
        


        public EmailSettingsServices(IRepository<EmailsSettings> EmailsSettingsRepo, IdcmsLogService dcmsLogService
       , IWorkContext workContext, EskaDCMSDbContext context, IServiceScopeFactory serviceScopeFactory)
        {
            _EmailsSettingsRepo = EmailsSettingsRepo;
            _workContext = workContext;  
            _context=context;
            _dcmsLogService = dcmsLogService;
            _assembly = Assembly.GetExecutingAssembly().GetName().Name;

        }
        public async Task<bool> UpsertEmailsSettigs(UpsertEmailSettingsVM EmailsSettigsRequestVm)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var DBTrans = await _context.Database.BeginTransactionAsync();
                try
                {

                    long SiteId = _workContext.GetCurrentSiteId();
                    bool IsEditMood = EmailsSettigsRequestVm.Id != 0;

                    // EmailSettings
                    var EmailsSettigs = new EmailsSettings();


                    if (IsEditMood)
                    {
                        EmailsSettigs = await _EmailsSettingsRepo
                                      .Query()
                                      .Where(m => m.Id == EmailsSettigsRequestVm.Id)
                                      .FirstOrDefaultAsync();

                    }
                    EmailsSettigs.Name = EmailsSettigsRequestVm.Name;
                    EmailsSettigs.Status = EmailsSettigsRequestVm.Status;
                    EmailsSettigs.SmtpSenderEmail = EmailsSettigsRequestVm.SmtpSenderEmail;
                    EmailsSettigs.SmtpSenderEmailAdmin = EmailsSettigsRequestVm.SmtpSenderEmailAdmin;

                    EmailsSettigs.RequiresAuthentication = EmailsSettigsRequestVm.RequiresAuthentication;

                    if (EmailsSettigsRequestVm.IsDefault)
                    {
                        var DefaultEmailsSettings = await _EmailsSettingsRepo
                                  .Query()
                                  .Where(m => m.SiteId == SiteId && m.IsDefault == true)
                                  .ToListAsync();

                        foreach (var DefaultEmailSettings in DefaultEmailsSettings)
                        {
                            DefaultEmailSettings.IsDefault = false;

                        }
                    }

                    EmailsSettigs.IsDefault = EmailsSettigsRequestVm.IsDefault;
                    EmailsSettigs.SmtpPassword = EmailsSettigsRequestVm.SmtpPassword;
                    EmailsSettigs.SmtpUsername = EmailsSettigsRequestVm.SmtpUsername;
                    EmailsSettigs.SiteId = SiteId;
                    EmailsSettigs.SmtpPort = EmailsSettigsRequestVm.SmtpPort;
                    EmailsSettigs.SmtpServer = EmailsSettigsRequestVm.SmtpServer;
                    if (!IsEditMood)
                    {
                        _EmailsSettingsRepo.Add(EmailsSettigs);
                    }
                    await _EmailsSettingsRepo.SaveChangesAsync();
                    await DBTrans.CommitAsync();

                    return true;


                }
                catch (Exception e)
                {
                    await DBTrans.RollbackAsync();
                    throw e;
                }
            });
        }

        public async Task<List<GetEmailResponseVm>> GetAllEmails()
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
            var EmailSettigs=  await _EmailsSettingsRepo.Query().Where(x =>x.Status!=EStatus.Deleted&&x.SiteId== SiteId)
                                                                         .Select(x => new GetEmailResponseVm
                                                                         {
                                                                             Id = x.Id,
                                                                             Name=x.Name,
                                                                             SmtpSenderEmail = x.SmtpSenderEmail,
                                                                             SmtpPort = x.SmtpPort,
                                                                             SmtpUsername = x.SmtpUsername,
                                                                             SmtpServer = x.SmtpServer,
                                                                             IsDefault = x.IsDefault,
                                                                             RequiresAuthentication = x.RequiresAuthentication,
                                                                             SmtpSenderEmailAdmin=x.SmtpSenderEmailAdmin
                                                                             

                                                                         })
                                                                         .AsNoTracking()
                                                                         .ToListAsync();
                return EmailSettigs;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<GetEmailResponseByIdVm> GetById(long Id)
        {
            try
            {
                var EmailSettigs = await _EmailsSettingsRepo.Query().Where(x => x.Id == Id)
                                                                         .Select(x => new GetEmailResponseByIdVm
                                                                         {
                                                                             Id = x.Id,
                                                                             SmtpSenderEmail = x.SmtpSenderEmail,
                                                                             Name=x.Name,
                                                                             SmtpPort = x.SmtpPort,
                                                                             SmtpUsername = x.SmtpUsername,
                                                                             SmtpServer = x.SmtpServer,
                                                                             IsDefault = x.IsDefault,
                                                                             RequiresAuthentication = x.RequiresAuthentication,
                                                                             SmtpPassword=x.SmtpPassword,
                                                                             SmtpSenderEmailAdmin = x.SmtpSenderEmailAdmin,
                                                                             Status = x.Status
                                                                             


                                                                         })
                                                                         .AsNoTracking()
                                                                         .FirstOrDefaultAsync();
                return EmailSettigs;
            }
            catch (Exception ex)
            {

                throw ex;
            }        
        }

        public async Task<bool> UpdateEmailSettingsStatus(long Id,EStatus NewStatus)
        {
            try
            {
                var EmailSetting = await _EmailsSettingsRepo.Query().Where(x => x.Id == Id).FirstOrDefaultAsync();
                EmailSetting.Status= NewStatus;
                _EmailsSettingsRepo.SaveChanges();

                return true;

            }
            catch (Exception)
            {

                throw;
            }        }

    
    }
}
