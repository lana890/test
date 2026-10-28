using EskaCMS.EmailSender.SMTP.Areas.ViewModels;
using EskaCMS.Infrastructure.Web.SmartTable;
using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Forms.Model.FormsViewModel
{

    public class FormDataViewModel
    {
        public long Id { get; set; }
        public string FormName { get; set; }
        public string FormData { get; set; }
        public string MailSender { get; set; }
        public List<string> MailReceiver { get; set; }
        public string MailReceiverstr { get; set; }
        public string MailSubject { get; set; }
        public string AttacheFolder { get; set; }
        public string MailMessage { get; set; }
        public List<string> SubmitterMailReceiver { get; set; }
        public string SubmitterMailReceiverstr { get; set; }
        public string SubmitterMailSubject { get; set; }
        public string SubmitterMailMessage { get; set; }
        public string SMSSender { get; set; }
        public string SMSReceiver { get; set; }
        public DateTimeOffset SubmitionDate { get; set; }
        public long SiteId { get; set; }
        public bool MailSendStatus { get; set; }
        public bool SubmitterMailSendStatus { get; set; }
        public DateTimeOffset? SubmitionDateFrom { get; set; }
        public DateTimeOffset? SubmitionDateTo { get; set; }

        // public string EmailTemplateId { get; set; };
        public EmailSenderVM EmailSenderVM { get; set; }
    }

    public class NewFormDataViewModel
    {
        public long Id { get; set; }
        public string FormName { get; set; }
        public string FormData { get; set; }
        public string MailSender { get; set; }
        public List<string> MailReceiver { get; set; }
        public string MailReceiverstr { get; set; }
        public string MailSubject { get; set; }
        public string AttacheFolder { get; set; }
        public string MailMessage { get; set; }
        public List<string> SubmitterMailReceiver { get; set; }
        public string SubmitterMailReceiverstr { get; set; }
        public string SubmitterMailSubject { get; set; }
        public string SubmitterMailMessage { get; set; }
        public string SMSSender { get; set; }
        public string SMSReceiver { get; set; }
        public DateTimeOffset SubmitionDate { get; set; }
        public long SiteId { get; set; }
        public bool MailSendStatus { get; set; }
        public bool SubmitterMailSendStatus { get; set; }
        public DateTimeOffset? SubmitionDateFrom { get; set; }
        public DateTimeOffset? SubmitionDateTo { get; set; }

        // public string EmailTemplateId { get; set; };
        public EmailSenderVM EmailSenderVM { get; set; }
    }

    public class GridOutputVM<T>
    {
        public List<T> Items { get; set; }
        public int TotalRecord { get; set; }
    }
    public class SearchTableParam<T>
    {
        public Pagination Pagination { get; set; }

        public T Search { get; set; }

    }
}
