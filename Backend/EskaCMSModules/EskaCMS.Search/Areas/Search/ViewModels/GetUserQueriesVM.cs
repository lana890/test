using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Search.Areas.Search.ViewModels
{
    public class GetUserQueriesVM
    {
        public string Name { get; set; }
        public string Api { get; set; }
        public string PageName { get; set; }
        public bool?  IsShared { get; set; }
        public EPlatformSource? Source { get; set; }

    }


    public class GetUserQueriesDto
    {
        public long Id { get; set; }
        public string Payload { get; set; }
        public string Name { get; set; }
        public string Api { get; set; }
        public string PageName { get; set; }
        public bool IsShared { get; set; }
        public EPlatformSource Source { get; set; }

    }
}
