using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Scripts
{
    public static class ProductcombinationScripts
    {

        public static string UpdateProductCombination = @"UPDATE catalog_productoptioncombination AS optioncomb
                                                        JOIN catalog_product AS prod ON optioncomb.ProductId = prod.Id and prod.Status <> 3
                                                        JOIN catalog_productlink AS prodlink ON prodlink.LinkedProductId = prod.Id AND prodlink.LinkType = 1
                                                        SET optioncomb.ParentProductId = prodlink.ProductId;";
    }
}
