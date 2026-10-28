using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Enum;
namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class WishListProductVM
    {
     
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public bool? IsFeatuerItem { get; set; }

        public ProductSearchSort Sort { get; set; }
    }
    public class UserWishListVM
    {
    
        public long ProductId { get; set; }
        
    }
}
