using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Customattributes;
using EskaCMS.Entities.Enum;

namespace EskaCMS.Entities.Helpers
{
    public static class DeliveryHelper
    {
        public static string GetDescription(this System.Enum value)
        {

            var field = value.GetType().GetField(value.ToString());
            var attribute = (DeliveryDescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DeliveryDescriptionAttribute));
            return attribute?.Description ?? value.ToString();
        }

    }
}
