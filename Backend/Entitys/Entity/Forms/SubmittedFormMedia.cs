using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Forms
{
    public class SubmittedFormMedia : EntityBase
    {
        public long SubmittedFormId { get; set; }
        public SubmittedForm SubmittedForm { get; set; }
        public long MediaId { get; set; }
        public Media Media { get; set; }
    }
}
