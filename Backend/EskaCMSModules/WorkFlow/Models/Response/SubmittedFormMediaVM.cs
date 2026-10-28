using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.WorkFlow.Models.Response
{
    public class SubmittedFormMediaVM
    {
        public long Id { get; set; }
        public string PublicUrl { get; set; }
        public long Size { get; set; }
        public string Filename { get; set; }
        public string ContentType { get; set; }
        public string? ReferenceLocation { get;  set; }
    }
}
