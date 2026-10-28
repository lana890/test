using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.EskaBilling.Models.response
{
    public class ShechduledPlansVM
    {
        public long CustomerSimId { get; set; }
        public long ProductId { get; set; }
        public string TargetProfile { get; set; }
        public string Note { get; set; }
        
        public CustomerSims CustomerSim { get; set; }
    }
}
