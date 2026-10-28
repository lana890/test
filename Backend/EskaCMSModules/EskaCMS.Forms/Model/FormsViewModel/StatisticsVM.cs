using EskaCMS.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Forms.Model.FormsViewModel
{
    public class StatisticsVM
    {
        public IList<KeyValueVm> KeyValue { get; set; }

        public TimeIntervals Timeinterval { get; set; }
        public GroupByEnum GroupBy { get; set; }
        public TypeMathematics TypeMathematics { get; set; }


        public long PageId { get; set; }
        public string ComponentsettingsId { get; set; }
        public long? UserId { get; set; }


    }
    public class KeyValueVm
    {
        public string KeySearch { get; set; }
        public string ValueSearch { get; set; }
        public ComparisonOperators ComparisonOperatorsSearch { get; set; }

        public string Key { get; set; }
        public string Value { get; set; }
        public ComparisonOperators ComparisonOperators { get; set; }


    }
}
