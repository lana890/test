using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Enum
{
    public class UserTaskEnums
    {

        public enum ETaskStatus
        {
            Proposed = 1,
            InProgress = 2,
            Completed = 3,
            Deleted = 4,
           
        }
        
        public enum ETaskPriority
        {
            Low = 1,
            Normal = 2,
            High = 3,
           
        }
        
        public enum ETaskPrivacy
        {
            Private = 1,
            FirnedsOnly = 2,
            Public = 3,
           
        }
    }
}
