using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace EskaCMS.Entities.Entity.EcomLog
{
    public class EcommerceLogsVersions : EntityBase
    {
        public long Id { get; set; }
        public long EcomLogId { get; set; }
        public long CreatedById { get; set; }
        public DateTime? ModificationDate { get; set; }
        //Request
        public string Request { get; set; }
        public string Header { get; set; }
        public string Method { get; set; }
        public string URL { get; set; }
        //Response
        public string Response { get; set; }
        public int? StatusCode { get; set; }
        public EcommerceLogs EcomLog { get; set; }
        public User CreatedBy { get; set; }
    }
}
