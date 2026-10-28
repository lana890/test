using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.ViewModels.Category.Responses
{
    public class CatelogFlatViewModel
    {
        public long Id { get; set; }
        public long? ParentId { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string PublicUrl { get; set; }
        public string Description { get; set; }
        public bool IsPromotional { get; set; }



    }
}
