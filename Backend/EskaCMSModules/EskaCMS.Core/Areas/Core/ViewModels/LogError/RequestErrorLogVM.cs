using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Areas.Core.ViewModels.LogError
{
    public class RequestErrorLogVM
    {
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public string SearchText { get; set; }
        public DateTimeOffset? FromDate { get; set; } = null;
        public DateTimeOffset? ToDate { get; set; } = null;
    }
}
