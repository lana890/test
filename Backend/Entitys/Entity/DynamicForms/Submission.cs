using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.DynamicForms
{
    public class Submission :EntityBase
    {
        public DateTimeOffset  CreationDate { get; set; }
        public long  CreatedById { get; set; }
        public User  CreatedBy { get; set; }
        public long  FormItemId { get; set; }
        public FormItem FormItem { get; set; }
        public FormItemType Type { get; set; }
        public long?  PageId { get; set; }
        public Page Page { get; set; }
        public long SiteId { get; set; }
        public Site Site { get; set; }

        public string Result { get; set; }
        public ICollection<SubmissionInfo>  SubmissionInfo { get; set; }
    }
}
