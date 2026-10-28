using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Scripts
{
    public class UpdateEcommerceTemplate
    {
        public static string UpdateCategory = @"update core_category  set CategoryTemplateId  = 1 where CategoryTemplateId=0 ";
        public static string UpdateProduct = @"update catalog_product  set ProductTemplateId  = 1 where ProductTemplateId =0 ";
        public static string UpdateBrand = @"update catalog_brand  set BrandTemplateId  = 1 where BrandTemplateId=0 ";

    }
}
