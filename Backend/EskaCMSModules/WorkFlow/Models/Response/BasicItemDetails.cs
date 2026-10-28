using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.WorkFlow.Models.Response
{
    public class BasicItemDetails
    {
        public DateTimeOffset CreationDate { get; set; }
        public string CreatedBy { get; set; }
        public string Note { get; set; }
     
    }
}
