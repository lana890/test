
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Components.Models
{
   
    public class ComponentsCategoriesViewModel
    {
        public CategoryComponentViewModel Category { get; set; }
        public IList<EskaCMS.Entities.Entity.Components.Component> Components { get; set; }
    }
}
