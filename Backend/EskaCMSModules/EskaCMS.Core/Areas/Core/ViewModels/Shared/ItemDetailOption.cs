using System.Collections.Generic;

namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class ItemDetailOption
    {
        public long OptionId { get; set; }

        public string OptionName { get; set; }

        public IList<string> Values { get; set; } = new List<string>();
    }


    public class ItemDetailOptionForAdmin
    {
        public long OptionId { get; set; } 
        public IList<OptionDetailForAdmin> OptionDetails { get; set; } = new List<OptionDetailForAdmin>();
        public IList<ValueItemForAdmin> Values { get; set; } = new List<ValueItemForAdmin>();

    }


    public class OptionDetailForAdmin
    {
        public long CultureId { get; set; }

        public string Name { get; set; }
       

    }


    public class ValueItemForAdmin
    {
        public long ValueId { get; set; }
        public IList<ValueDetailForAdmin> ValueDetails { get; set; } = new List<ValueDetailForAdmin>();

    }
    
    public class ValueDetailForAdmin
    {
        public long CultureId { get; set; }
        public string Value { get; set; }

    }



}
