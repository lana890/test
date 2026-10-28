using EskaCMS.Entities.Entity.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.DynamicForms.ViewModels
{
    public class CategoryVM
    {
        public long Id { get; set; }

        public IList<ChildDetailsMainInfoVM> CategoryDetails { get; set; }
        public IList<CategoryVM> Children { get; set; }
        public CategoryVM()
        {
            CategoryDetails = new List<ChildDetailsMainInfoVM>();
        }
    }
}
