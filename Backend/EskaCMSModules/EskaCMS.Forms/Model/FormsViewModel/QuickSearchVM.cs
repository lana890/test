using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Forms.Model.FormsViewModel
{
    public class QuickSearchVM
    {
        public long Id { get; set; }
        public string FormName { get; set; }
        public string FormData { get; set; }
        public string MailSender { get; set; }
        public List<string> MailReceiver { get; set; }
        public List<string> SubmitterMailReceiver { get; set; }
        public bool MailSendStatus { get; set; }
        public bool SubmitterMailSendStatus { get; set; }
        public DateTimeOffset SubmitionDate { get; set; }
    }
}
