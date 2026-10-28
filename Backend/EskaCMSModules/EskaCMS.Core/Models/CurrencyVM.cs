using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Models
{
    public class CurrencyVM
    {
        public long Id { get; set; }
        public string Name { get; set; }

        public string CultureCode { get; set; }
    }

    public class CurrencyByCultureVM
    {
  
        public decimal Rate { get; set; }

        public string Code { get; set; }
    }
}
