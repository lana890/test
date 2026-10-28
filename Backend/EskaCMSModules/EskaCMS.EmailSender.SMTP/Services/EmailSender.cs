using EskaCMS.Core.Areas.Core.ViewModels.AppSettingsModels;
using EskaCMS.Core.Model;
using EskaCMS.Core.Services;
using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static BusinessModels.BusinessModel;

namespace EskaCMS.EmailSender.SMTP.Services
{
    public class EmailSender : IEmailSender
    {

        private readonly string _assembly;
        private readonly IdcmsLogService _dcmsLogService;
        //private readonly IEmailTemplateService _emailTemplateService;
        private readonly IConfiguration _config;
        private readonly ISiteSettingsService _siteSettingsService;


        public EmailSender(
            IConfiguration config,
            IdcmsLogService dcmsLogService,
            IEmailTemplateService emailTemplateService,
            ISiteSettingsService siteSettingsService

            )
        {
            _dcmsLogService = dcmsLogService;
            // _emailTemplateService = emailTemplateService;
            _assembly = Assembly.GetExecutingAssembly().GetName().Name;
            _siteSettingsService = siteSettingsService;
            _config = config;
        }


        public async Task SendEmailAsync(
       List<string> Emails,
       string SenderModule,
       string subject,
       string body,
       long SiteId,
       EmailsSettings emailSettings,
       bool isHtml = true,
       List<FileViewModel> attachment = null,
       List<string> CC = null,
       List<FileMemoryViewModel> fileMemoryViewModels=null)
        {
            try
            {
                if (emailSettings == null)
                {
                    emailSettings = await _siteSettingsService.GetEmailSetting(SiteId);
                }

                var message = new MimeMessage();
                var builder = new BodyBuilder();

                message.From.Add(MailboxAddress.Parse(emailSettings.SmtpSenderEmail));

                foreach (var email in Emails)
                {
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        message.To.Add(MailboxAddress.Parse(email));
                    }
                }

                if (CC != null && CC.Any())
                {
                    foreach (var ccEmail in CC)
                    {
                        if (!string.IsNullOrWhiteSpace(ccEmail))
                        {
                            message.Cc.Add(MailboxAddress.Parse(ccEmail));
                        }
                    }
                }

                message.Subject = subject;

                // BODY
                if (isHtml)
                    builder.HtmlBody = body;
                else
                    builder.TextBody = body;

                // ATTACHMENTS
                if (attachment != null && attachment.Any())
                {
                    foreach (var item in attachment)
                    {
                        string filePath;

                        if (item.publicUrl.Contains(item.fileName))
                        {
                            filePath = Path.Combine(
                                Directory.GetCurrentDirectory(),
                                "wwwroot",
                                item.publicUrl.Replace("/", "\\"));
                        }
                        else
                        {
                            filePath = Path.Combine(
                                Directory.GetCurrentDirectory(),
                                item.publicUrl,
                                item.fileName);
                        }

                        if (File.Exists(filePath))
                        {
                            builder.Attachments.Add(filePath);
                        }
                    }
                }
                if (fileMemoryViewModels != null && fileMemoryViewModels.Any())
                {
                    foreach (var file in fileMemoryViewModels)
                    {
                        if (file == null || file.FileBytes == null || file.FileBytes.Length == 0 || string.IsNullOrWhiteSpace(file.FileName))
                            continue;

                        builder.Attachments.Add(
                            file.FileName,
                            file.FileBytes,
                            ContentType.Parse(string.IsNullOrWhiteSpace(file.ContentType)
                                ? "application/octet-stream"
                                : file.ContentType));
                    }
                }

                message.Body = builder.ToMessageBody();

                await Send(emailSettings, message);
            }
            catch (Exception exc)
            {
                await _dcmsLogService.AddError(
                    _assembly,
                    exc.Message,
                    "Failed to Send email: " + JsonConvert.SerializeObject(Emails),
                    exc.Message,
                    null);

                throw;
            }
        }


        public async Task SendMeetingEmailAsync(string OrganizerEmail,string OrganizerName, List<string> Atendees, string subject, string body,string location,DateTimeOffset Start, DateTimeOffset End, long SiteId, bool isHtml = true, List<FileViewModel> attachment = null)
        {
            try
            {
                EmailsSettings emailSettings = await _siteSettingsService.GetEmailSetting(SiteId);
               
                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(emailSettings.SmtpSenderEmail));
                foreach (var email in Atendees)
                {
                    message.To.Add(MailboxAddress.Parse(email));
                }
                message.Cc.Add(MailboxAddress.Parse(OrganizerEmail));
                message.Subject = subject;

                // Create the calendar event
                var calendarEvent = CreateCalendarEvent(OrganizerName,OrganizerEmail,subject, location, Atendees, Start, End);

                var multipart = new Multipart("mixed");

                // Create the plain text or HTML body part
                var bodyPart = new TextPart(isHtml ? TextFormat.Html : TextFormat.Plain)
                {
                    Text = body
                };

                // Create the calendar part
                var calendarPart = new TextPart("calendar")
                {
                    Text = calendarEvent,
                    ContentTransferEncoding = ContentEncoding.Base64
                };
                calendarPart.ContentType.MediaType = "text";
                calendarPart.ContentType.MediaSubtype = "calendar";
                calendarPart.ContentType.Parameters.Add("method", "REQUEST");
                calendarPart.ContentType.Parameters.Add("name", "invite.ics");

                multipart.Add(bodyPart);
                multipart.Add(calendarPart);

                // Add any attachments
                if (attachment != null)
                {
                    foreach (var item in attachment)
                    {
                        var file = item.publicUrl.Contains(item.fileName)
                            ? Path.Combine(Directory.GetCurrentDirectory().Trim(), "wwwroot\\", item.publicUrl.Replace("/", "\\"))
                            : Path.Combine(Directory.GetCurrentDirectory().Trim(), item.publicUrl, item.fileName);
                        var attachmentPart = new MimePart()
                        {
                            Content = new MimeContent(File.OpenRead(file)),
                            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                            ContentTransferEncoding = ContentEncoding.Base64,
                            FileName = item.fileName
                        };
                        multipart.Add(attachmentPart);
                    }
                }

                message.Body = multipart;
                await Send(emailSettings, message);
            }
            catch (Exception exc)
            {
                await _dcmsLogService.AddError(_assembly, exc.Message, "Failed to Send email: " + JsonConvert.SerializeObject(Atendees), exc.Message, null);
                throw exc;
            }
        }

        private string CreateCalendarEvent(string OrganizerName, string OrganizerEmail, string subject, string location,List<string> attendees, DateTimeOffset start, DateTimeOffset end)
        {
            var sb = new StringBuilder();

            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("VERSION:2.0");
            sb.AppendLine("PRODID:-//Your Organization//Your Product//EN");
            sb.AppendLine("METHOD:REQUEST");
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"DTSTART:{start.ToUniversalTime():yyyyMMddTHHmmssZ}");
            sb.AppendLine($"DTEND:{end.ToUniversalTime():yyyyMMddTHHmmssZ}");
            sb.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
            sb.AppendLine($"ORGANIZER;CN=\"{OrganizerName}\":mailto:{OrganizerEmail}");
            sb.AppendLine($"UID:{Guid.NewGuid()}");

            // Add each attendee
            foreach (var attendee in attendees)
            {
                sb.AppendLine($"ATTENDEE;PARTSTAT=NEEDS-ACTION;RSVP=TRUE;CN=\"{attendee}\":mailto:{attendee}");
            }
            sb.AppendLine($"ATTENDEE;PARTSTAT=NEEDS-ACTION;RSVP=TRUE;CN=\"{OrganizerName}\":mailto:{OrganizerEmail}");

            sb.AppendLine($"SUMMARY:{subject}");
            sb.AppendLine($"LOCATION:{location}");
            sb.AppendLine("BEGIN:VALARM");
            sb.AppendLine("TRIGGER:-PT15M"); 
            sb.AppendLine("ACTION:DISPLAY");
            sb.AppendLine("DESCRIPTION:Reminder");
            sb.AppendLine("END:VALARM");
            sb.AppendLine("END:VEVENT");
            sb.AppendLine("END:VCALENDAR");

            return sb.ToString();
        }
        public async Task<bool> TestEmailSettings(TestEmailSettingsVM testEmailSettingsVM, long SiteId)
        {
            try
            {
                EmailsSettings emailConfig = new()
                {
                    RequiresAuthentication = testEmailSettingsVM.RequiresAuthentication,
                    SmtpSenderEmail = testEmailSettingsVM.SmtpSenderEmail,
                    SmtpPassword = testEmailSettingsVM.SmtpPassword,
                    SmtpPort = testEmailSettingsVM.SmtpPort,
                    SmtpServer = testEmailSettingsVM.SmtpServer,
                    SmtpUsername = testEmailSettingsVM.SmtpUsername
                };

                List<string> EmailTo = new();

                EmailTo.Add(testEmailSettingsVM.EmailTo);

                await SendEmailAsync(EmailTo, "", "!Important News.", testEmailSettingsVM.Body, SiteId, emailConfig, true, null);

                return true;

            }
            catch (Exception ex)
            {

                throw ex;
                return false;
            }
        }

  
        private async Task Send(EmailsSettings emailSettings, MimeMessage message)
        {
            using (var client = await CreateConnectedClientAsync(emailSettings))
            {
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }

        /// <summary>
        /// Opens and authenticates a single SMTP connection. Callers are responsible for
        /// disconnecting/disposing. Used both for one-off sends and for bulk sends that
        /// reuse the same connection across many messages.
        /// </summary>
        private async Task<SmtpClient> CreateConnectedClientAsync(EmailsSettings emailSettings)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            var client = new SmtpClient();

            // Accept all SSL certificates (in case the server supports STARTTLS)
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            // in Production SecureSocketOptions.None should be changed to Auto
            var secureSocketOptions = (SecureSocketOptions)_config.GetValue<int>("SmtpSecureSocketOptions");
            await client.ConnectAsync(emailSettings.SmtpServer, emailSettings.SmtpPort, secureSocketOptions);

            // Note: since we don't have an OAuth2 token, disable the XOAUTH2 authentication mechanism.
            client.AuthenticationMechanisms.Remove("XOAUTH2");

            if (emailSettings.RequiresAuthentication)
                await client.AuthenticateAsync(emailSettings.SmtpUsername, emailSettings.SmtpPassword);

            return client;
        }

        /// <summary>
        /// Sends an individual email to every recipient (each recipient only ever sees their
        /// own address in the "To" field). Designed for large recipient lists such as
        /// newsletters: the SMTP connection is reused across a whole batch instead of being
        /// re-opened per message, attachments are read from disk once, batches are throttled
        /// to respect provider rate limits, and a single bad address never aborts the run.
        /// </summary>
        public async Task<(int SuccessCount, int FailureCount)> SendBulkIndividualAsync(
            List<string> Emails,
            string SenderModule,
            string subject,
            string body,
            long SiteId,
            EmailsSettings emailSettings,
            bool isHtml = true,
            List<FileViewModel> attachment = null,
            List<string> CC = null,
            List<FileMemoryViewModel> fileMemoryViewModels = null,
            int batchSize = 100,
            int delayMsBetweenBatches = 1000)
        {
            int success = 0;
            int failure = 0;

            if (Emails == null || Emails.Count == 0)
                return (0, 0);

            if (emailSettings == null)
                emailSettings = await _siteSettingsService.GetEmailSetting(SiteId);

            var recipients = Emails
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (recipients.Count == 0)
                return (0, 0);

            if (batchSize <= 0)
                batchSize = 100;

            // Read attachments (disk + in-memory) ONCE and reuse the bytes for every message,
            // instead of hitting the filesystem once per recipient.
            var preparedAttachments = PrepareAttachments(attachment, fileMemoryViewModels);

            for (int offset = 0; offset < recipients.Count; offset += batchSize)
            {
                var batch = recipients.Skip(offset).Take(batchSize).ToList();
                SmtpClient client = null;

                try
                {
                    client = await CreateConnectedClientAsync(emailSettings);

                    foreach (var recipient in batch)
                    {
                        try
                        {
                            if (!client.IsConnected)
                            {
                                client.Dispose();
                                client = await CreateConnectedClientAsync(emailSettings);
                            }

                            var message = BuildMessage(
                                emailSettings.SmtpSenderEmail,
                                new[] { recipient },
                                CC,
                                subject,
                                body,
                                isHtml,
                                preparedAttachments);

                            await client.SendAsync(message);
                            success++;
                        }
                        catch (Exception exc)
                        {
                            failure++;
                            await _dcmsLogService.AddError(
                                _assembly,
                                exc.Message,
                                "Failed to Send email to: " + recipient,
                                exc.Message,
                                SiteId);
                        }
                    }
                }
                catch (Exception exc)
                {
                    // Batch-level failure (e.g. cannot connect at all) — nothing in this batch
                    // was sent, so count them as failures and move on to the next batch.
                    failure += batch.Count;
                    await _dcmsLogService.AddError(
                        _assembly,
                        exc.Message,
                        "Failed to send email batch (recipients " + offset + "-" + (offset + batch.Count - 1) + ")",
                        exc.Message,
                        SiteId);
                }
                finally
                {
                    if (client != null)
                    {
                        try
                        {
                            if (client.IsConnected)
                                await client.DisconnectAsync(true);
                        }
                        catch { /* best-effort disconnect */ }

                        client.Dispose();
                    }
                }

                // Throttle between batches so we don't trip the provider's rate limit.
                if (delayMsBetweenBatches > 0 && offset + batchSize < recipients.Count)
                    await Task.Delay(delayMsBetweenBatches);
            }

            return (success, failure);
        }

        /// <summary>
        /// Reads every attachment (disk-backed and in-memory) into memory a single time so the
        /// bytes can be reused across many messages in a bulk send.
        /// </summary>
        private List<(string FileName, byte[] Data, string ContentType)> PrepareAttachments(
            List<FileViewModel> attachment,
            List<FileMemoryViewModel> fileMemoryViewModels)
        {
            var prepared = new List<(string FileName, byte[] Data, string ContentType)>();

            if (attachment != null && attachment.Any())
            {
                foreach (var item in attachment)
                {
                    string filePath = item.publicUrl.Contains(item.fileName)
                        ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", item.publicUrl.Replace("/", "\\"))
                        : Path.Combine(Directory.GetCurrentDirectory(), item.publicUrl, item.fileName);

                    if (File.Exists(filePath))
                        prepared.Add((item.fileName, File.ReadAllBytes(filePath), null));
                }
            }

            if (fileMemoryViewModels != null && fileMemoryViewModels.Any())
            {
                foreach (var file in fileMemoryViewModels)
                {
                    if (file == null || file.FileBytes == null || file.FileBytes.Length == 0 || string.IsNullOrWhiteSpace(file.FileName))
                        continue;

                    prepared.Add((file.FileName, file.FileBytes, file.ContentType));
                }
            }

            return prepared;
        }

        /// <summary>
        /// Builds a single MimeMessage from already-prepared parts (recipients, body, cached attachments).
        /// </summary>
        private MimeMessage BuildMessage(
            string from,
            IEnumerable<string> to,
            List<string> cc,
            string subject,
            string body,
            bool isHtml,
            List<(string FileName, byte[] Data, string ContentType)> attachments)
        {
            var message = new MimeMessage();
            var builder = new BodyBuilder();

            message.From.Add(MailboxAddress.Parse(from));

            foreach (var email in to)
            {
                if (!string.IsNullOrWhiteSpace(email))
                    message.To.Add(MailboxAddress.Parse(email));
            }

            if (cc != null && cc.Any())
            {
                foreach (var ccEmail in cc)
                {
                    if (!string.IsNullOrWhiteSpace(ccEmail))
                        message.Cc.Add(MailboxAddress.Parse(ccEmail));
                }
            }

            message.Subject = subject;

            if (isHtml)
                builder.HtmlBody = body;
            else
                builder.TextBody = body;

            if (attachments != null && attachments.Any())
            {
                foreach (var a in attachments)
                {
                    if (string.IsNullOrWhiteSpace(a.ContentType))
                        builder.Attachments.Add(a.FileName, a.Data);
                    else
                        builder.Attachments.Add(a.FileName, a.Data, ContentType.Parse(a.ContentType));
                }
            }

            message.Body = builder.ToMessageBody();
            return message;
        }

    }


}
