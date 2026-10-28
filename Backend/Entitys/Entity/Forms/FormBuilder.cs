using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EskaCMS.Entities.Entity.Forms
{
    public class FormBuilder : EntityBase
    {
        public string FormName { get; set; }
        public string FormData { get; set; }
        public string MailSender { get; set; }
        public string MailReceiver { get; set; }
        public string MailSubject { get; set; }
        public bool MailSendStatus { get; set; }       
        public string MailMessage { get; set; }
        public string SubmitterMailReceiver { get; set; }
        public string SubmitterMailSubject { get; set; }
        public string SubmitterMailMessage { get; set; }
        public bool SubmitterMailSendStatus { get; set; }
        public string SMSSender { get; set; }
        public string SMSReceiver { get; set; }
        public bool SMSSendStatus { get; set; }
        public string AttacheFolder { get; set; }
        public string FormFiles { get; set; }
        public DateTimeOffset SubmitionDate { get; set; }
        public long SiteId { get; set; }

    }
}
