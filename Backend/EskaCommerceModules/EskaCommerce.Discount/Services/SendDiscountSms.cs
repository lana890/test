using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Discount.Services
{
    public class SendDiscountSms : ISendDiscountSms
    {
        public IServiceScopeFactory ServiceScopeFactory { get; }

        public SendDiscountSms(IServiceScopeFactory serviceScopeFactory)
        {
            ServiceScopeFactory = serviceScopeFactory;
        }

        // Compile these constant-pattern regexes once instead of rebuilding them on every
        // HtmlToPlainText call. Same patterns/options -> identical matching behavior.
        private static readonly Regex _lineBreakRegex = new Regex(@"<(br|BR)\s{0,1}\/{0,1}>", RegexOptions.Multiline);
        private static readonly Regex _stripFormattingRegex = new Regex(@"<[^>]*(>|$)", RegexOptions.Multiline);
        private static readonly Regex _tagWhiteSpaceRegex = new Regex(@"(>|$)(\W|\n|\r)+<", RegexOptions.Multiline);


        public async Task SendMessageAsync(NotificationType notificationType, EmailsSettings EmailSettings = null, NotificationObject data = null, long siteId = 0)
        {
            using (var scope = ServiceScopeFactory.CreateScope())
            {
                try
                {
                    var SmsSender = scope.ServiceProvider.GetRequiredService<ISmsSender>();
                    var _EmailTemplate = scope.ServiceProvider.GetRequiredService<IRepositoryWithCustomId<EmailTemplates>>();

                    if (notificationType == NotificationType.SMS)
                    {
                        if (data.EmailOptions != null && data.EmailOptions.Params != null)
                        {
                            var template = await _EmailTemplate.Query().Where(x => x.Id == data.EmailOptions.TemplateId).Select(x => new EmailTemplateVM
                            {
                                Id = x.Id,
                                CultureId = x.CultureId,
                                EmailTemplateTypeId = x.EmailTemplateTypeId,
                                IsHTML = x.IsHTML,
                                SiteId = x.SiteId,
                                Subject = x.Subject,
                                EmailBody = x.EmailBody,
                                AdditionalInfo = x.AdditionalInfo


                            }).AsNoTracking().FirstOrDefaultAsync();

                            data = await ReplaceTemplateParams(data, template);
                          
                            data.MessageBody = HtmlToPlainText(data.MessageBody);

                        }

                        //foreach (string value in data.SendValue)
                        //{
                        //    var smsProvider = data.SMSProvider != null ? data.SMSProvider : SMSProviders.SMSMashastra;

                        //    try
                        //    {
                        //        await SmsSender.SendSmsAsync(smsProvider.Value, value, data.MessageBody, siteId);

                        //    }
                        //    catch (Exception e)
                        //    {
                        //        throw e;
                        //    }
                        //}
                    }

                }
                catch (System.Exception ex)
                {


                    throw ex;
                }
            }
        }
        public async Task<NotificationObject> ReplaceTemplateParams(NotificationObject data, EmailTemplateVM emailTemplate)
        {


            if (emailTemplate != null)
            {


                if (data.EmailOptions.Params.Count() > 0)
                {
                    foreach (var item in data.EmailOptions.Params)
                    {
                        emailTemplate.EmailBody = emailTemplate.EmailBody.Replace(item.Param, item.Value);
                    }

                    data.MessageTitle = emailTemplate.Subject;
                    data.MessageBody = emailTemplate.EmailBody;
                    data.isHTML = true;
                }
                else
                {
                    data.isHTML = emailTemplate.IsHTML;
                }
            }

            return data;

        }
        private string HtmlToPlainText(string html)
        {
            var lineBreakRegex = _lineBreakRegex;
            var stripFormattingRegex = _stripFormattingRegex;
            var tagWhiteSpaceRegex = _tagWhiteSpaceRegex;

            var text = html;
            //Decode html specific characters
            text = System.Net.WebUtility.HtmlDecode(text);
            //Remove tag whitespace/line breaks
            text = tagWhiteSpaceRegex.Replace(text, "><");
            //Replace <br /> with line breaks
            text = lineBreakRegex.Replace(text, Environment.NewLine);
            //Strip formatting
            text = stripFormattingRegex.Replace(text, string.Empty);
            text = System.Net.WebUtility.HtmlDecode(text);


            return text;
        }
    }
}
