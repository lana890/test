using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Customattributes
{
    public class DeliveryDescriptionAttribute : Attribute
    {
        public string Description { get; }

        public DeliveryDescriptionAttribute(string description)
        {
            Description = description;
        }
    }
}
