using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.ViewModels.Category.Responses
{
    public class CategoriesBreadCrumbVM
    {
        public long ParentId { get; set; }
        public long CategoryId { get; set; }

        public string Name { get; set; }
        public string Slug { get; set; }

    }
    //public class CategoriesBreadCrumbDetailsViewModel

    //{
    //    public string Slug { get; set; }
    //    public long Id { get; set; }
    //    public string Name { get; set; }
    //    public long? ParentId { get; set; }
    //}
}
