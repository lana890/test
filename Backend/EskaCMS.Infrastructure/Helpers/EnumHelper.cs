using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace EskaCMS.Infrastructure.Helpers
{
    public static class EnumHelper
    {
        public static IDictionary<Enum, string> ToDictionary(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var dics = new Dictionary<Enum, string>();
            var enumValues = Enum.GetValues(type);

            foreach (Enum value in enumValues)
            {
                dics.Add(value, GetDisplayName(value));
            }

            return dics;
        }

        public static string GetDisplayName(this Enum value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var displayName = value.ToString();
            var fieldInfo = value.GetType().GetField(displayName);
            if (fieldInfo == null)
            {
                return displayName;
            }
            var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes.Length > 0)
            {
                displayName = attributes[0].Description;
            }

            return displayName;
        }
    }
}
