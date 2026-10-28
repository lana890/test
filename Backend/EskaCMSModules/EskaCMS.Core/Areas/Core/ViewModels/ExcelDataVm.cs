using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class ExcelDataVm
    {
        public List<KeyValuePair>? Headers { get; set; }
        public List<ExcelSectionVm> Sections { get; set; }
    }

    public class ExcelSectionVm
    {
        public string SectionName { get; set; }
        public List<ExpandoObject> Content { get; set; }


    }
    public class KeyValuePair
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
