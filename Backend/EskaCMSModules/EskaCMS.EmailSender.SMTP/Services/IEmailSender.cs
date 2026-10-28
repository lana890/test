using EskaCMS.Core.Areas.Core.ViewModels.AppSettingsModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Model;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using System.Collections.Generic;

using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;
using System;

namespace EskaCMS.EmailSender.SMTP.Services
{
    public interface IEmailSender
    {
        Task SendMeetingEmailAsync(string OrganizerEmail, string OrganizerName, List<string> Atendees, string subject, string body, string location, DateTimeOffset Start, DateTimeOffset End, long SiteId, bool isHtml = true, List<FileViewModel> attachment = null);
        Task<bool> TestEmailSettings(TestEmailSettingsVM testEmailSettingsVM, long SiteId);
        Task SendEmailAsync(List<string> Emails, string SenderModule, string subject, string body, long SiteId, EmailsSettings emailSettings, bool isHtml = true, List<FileViewModel> attachment = null,List<string> CC=null,List<FileMemoryViewModel> fileMemoryViewModels=null);
        Task<(int SuccessCount, int FailureCount)> SendBulkIndividualAsync(List<string> Emails, string SenderModule, string subject, string body, long SiteId, EmailsSettings emailSettings, bool isHtml = true, List<FileViewModel> attachment = null, List<string> CC = null, List<FileMemoryViewModel> fileMemoryViewModels = null, int batchSize = 100, int delayMsBetweenBatches = 1000);
        //Task SendEmailAsync(EmailSenderVM model,long SiteId);
    }
}