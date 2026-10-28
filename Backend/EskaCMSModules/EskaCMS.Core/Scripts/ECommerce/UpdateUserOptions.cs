using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Scripts.ECommerce
{
    public static class UpdateUserOptions
    {
       
        public static string UpdateNormalizedPropertyNameAndValue = @" UPDATE core_useroptions
SET                                                                     NormalizedPropertyName = UPPER(PropertyName),
                                                                        NormalizedValue = UPPER(Value) where Id > 0;";

    }
}
