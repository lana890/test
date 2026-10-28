using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Menus.Models
{
    public class ParamsViewModel
    {
        public long? Id { get; set; }
        public string Value { get; set; }
        public string Key { get; set; }
        public int Order { get; set; }

    }
}
