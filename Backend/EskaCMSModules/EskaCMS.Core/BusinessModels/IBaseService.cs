using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Core.BusinessModels
{
   public class IBaseService
    {
       public long UserId { get; set; }
        public long SiteId { get; set; }
        public long CompanyId { get; set; }
    }
}
