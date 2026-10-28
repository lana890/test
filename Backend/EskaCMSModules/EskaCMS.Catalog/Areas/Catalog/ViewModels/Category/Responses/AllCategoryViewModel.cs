using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Responses
{
    public class AllCategoryViewModel
    {
        public long Id { get; set; }
        public long  CategoryTypeId { get; set; }
        public EStatus  Status { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsPromotional { get; set; }


        public List<CategoryDetailsViewModel> CategoryDetails { get; set; }
    }


    public class CategoryDetailsViewModel
    {
    
        public long Id { get; set; }

        public string Name { get; set; }

        public SlugInfo? SlugInfo { get; set; } = new SlugInfo();

        public SitesCultures? Culture { get; set; }=new SitesCultures();


    }
}
