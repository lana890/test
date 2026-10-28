using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Areas.Core.ViewModels.LogError
{
    public class ResponseLogErrorVM
    {
        public long Id { get; set; }
        public string Module { get; set; }

        public string Scope { get; set; }
        public string exception { get; set; }
        public string Message { get; set; }
        public string AdditionalInfo { get; set; }
        public EPlatformSource Channel { get; set; }
        public DateTimeOffset Date { get; set; }
        public long? UserId { get;  set; }
        public string UserName { get;  set; }
    }
}
