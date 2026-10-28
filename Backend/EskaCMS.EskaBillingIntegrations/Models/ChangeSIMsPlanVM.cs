using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.EskaBilling.Enums.IOTEnums;

namespace EskaCMS.EskaBilling.Models
{
    public class ChangeSIMsPlanVM
    {
        public List<long> Ids { get; set; }
        public long  ProductId { get; set; }
        public string Note { get; set; }
    }
}
