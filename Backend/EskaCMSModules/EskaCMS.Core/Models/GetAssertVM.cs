using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Models
{
    public class GetAssertVM
    {
        public EAssetsType? Type { get; set; } = null;

        public int PageSize { get; set; }
        public int PageNumber { get; set; }

        public string SearchText { get; set; } = null;
    }
}
