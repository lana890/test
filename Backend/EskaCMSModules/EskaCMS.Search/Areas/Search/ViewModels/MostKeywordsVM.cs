
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Search.Areas.Search.ViewModels
{
    public class MostKeywordsVM
    {

        public string Keyword { get; set; }
        public int Count { get; set; }

        public int ResultCount { get; set; }
    }
}
