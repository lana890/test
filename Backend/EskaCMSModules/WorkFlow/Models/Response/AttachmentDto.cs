using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.WorkFlow.Models.Response
{
    public class AttachmentDto
    {
        public long Id { get; set; }
        public string PublicUrl { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
        public string ReferenceLocation { get; set; }
    }
}
