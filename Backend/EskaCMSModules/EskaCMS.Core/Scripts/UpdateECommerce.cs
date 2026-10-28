using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Scripts
{
    public class UpdateECommerce
    {

        public static string UpdateCultureParentId = @"update core_category  set CultureParentId  = Id where CultureParentId is not null ";
        public static string UpdateparentId = @"update catalog_brand  set ParentId  = Id where ParentId is not null ";


}
}
