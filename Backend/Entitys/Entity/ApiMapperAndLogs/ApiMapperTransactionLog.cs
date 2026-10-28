using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.ApiMapperAndLogs
{
    public class ApiMapperTransactionLog :EntityBase
    {

        public string Request { get; set; }

        public string Response { get; set; }

        public string Headers { get; set; }
        
        public string Token { get; set; }

        public long ApiMapperId { get; set; }

        public ApiMapper ApiMapper { get; set; }

        public long CreatedById { get; set; }

        public DateTimeOffset CreationDate { get; set; }

        public User CreatedBy { get; set; }


    }
}
