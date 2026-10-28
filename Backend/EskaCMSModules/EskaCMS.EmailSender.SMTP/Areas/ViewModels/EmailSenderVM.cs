using EskaCMS.Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.EmailSender.SMTP.Areas.ViewModels
{
    public class EmailSenderVM
    {
        public EmailSenderVM()
        {
            Attachment = new List<FileViewModel>();
            Params = new List<EmailParamsVM>();
            Emails = new List<string>();
        }
        public string SenderModule { get; set; } // The SMTP account for the same website, since some sites may have two different account to send emails
        public string TemplateId { get; set; }
        public List<EmailParamsVM> Params { get; set; }
        public List<FileViewModel> Attachment { get; set; }

        public List<string> Emails { get; set; }
    }
    public class FileMemoryViewModel
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] FileBytes { get; set; }
        public long Length { get; set; }
    }
}

