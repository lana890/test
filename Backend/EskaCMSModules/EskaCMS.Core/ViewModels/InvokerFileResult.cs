using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.ViewModels
{
    public class InvokerFileResult
    {
        public InvokerFileResult()
        {

        }
        public InvokerFileResult(string Name, byte[] FileBytes, string ContentType)
        {
            this.Name = Name;
            this.FileBytes = FileBytes;
            this.ContentType = ContentType;
        }
        public byte[] FileBytes { get; set; }
        public string ContentType { get; set; }
        public string Name { get; set; }
    }
}
