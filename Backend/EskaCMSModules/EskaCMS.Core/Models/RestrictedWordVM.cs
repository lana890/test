using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Models
{
    public class RestrictedWordVM
    {
        public long Id { get; set; }
        public string RestrictedText { get; set; }
    }

    public class AllRestrictedWordVM
    {
        public long Id { get; set; }
        public string RestrictedText { get; set; }
        public string CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public EStatus Status { get; set; }
    }

    public class GetAllRestrictedWordFilter
    {
        public string RestrictedText { get; set; }
    }

    public class IsRestrictedWordVM
    {
        public string Text { get; set; }
    }
}
