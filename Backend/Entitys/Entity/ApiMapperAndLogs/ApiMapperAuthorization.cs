using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Core
{
   public class ApiMapperAuthorization : EntityBase
    {
        public long ApiMapperId { get; set; }
        public ApiMapper ApiMapper { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public ETokenSource? TokenSource { get; set; }
            
    }
}
