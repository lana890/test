using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Events
{
    public class SearchQuery : INotification
    {

        public string SearchText { get; set; }
        public int ResultCount { get; set; }
        public long SiteId { get; set; }
        
    }
}
