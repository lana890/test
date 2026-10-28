using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Entities.Entity.DynamicForms
{
    public class CategoryCriteria : EntityBase
    {
        public long CriteriaId { get; set; }
        public Criteria Criteria { get; set; }
        
        public long SubCategoryId { get; set; }
        public Category SubCategory { get; set; }// Petrol / Hyprid .....
    }
}
