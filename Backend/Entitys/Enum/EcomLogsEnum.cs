using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Entities.Enum
{
   
        public enum EHttpVerb
        {
            GET = 1,
            POST = 2,
            PUT = 3,
            DELETE = 4,

        }
        public enum TransactionStatus
        {
            InProgress = 1,
            Success = 2,
            Failed = 3,
            Retried = 4,
            Cancelled = 5
        }
    
}
