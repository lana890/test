using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.EskaBilling.Models
{
    public class UpdateBulkSimsVM
    {
        public List<long> SimsId { get; set; }
        public string SimName { get; set; }
        public string SimTag { get; set; }
    }
}
