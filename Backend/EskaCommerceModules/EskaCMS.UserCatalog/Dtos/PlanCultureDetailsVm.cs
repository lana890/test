using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.UserCatalog.Dtos
{
    public class PlanCultureDetailsVm
    {
        public long? Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ShortDescription { get; set; }
        public long CultureId { get; set; }
        public string CultureCode { get; set; }
    }
}
