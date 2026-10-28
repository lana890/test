using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.EskaBilling.Enums.IOTEnums;

namespace EskaCMS.EskaBilling.Models.response
{
    public class ChartViewModel
    {
        public int RateChanges { get; set; }

        public EIOTSIMStatus status { get; set; }

    }

 
}
