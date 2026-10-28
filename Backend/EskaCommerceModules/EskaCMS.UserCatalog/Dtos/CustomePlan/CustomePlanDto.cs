using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.UserCatalog.Dtos.CustomePlan
{
    public class CustomePlanDto
    {

        public long CustomePlanId { get; set; }
        public PlansVersionsCustomeDTO  PlansVersionsCustome { get; set; }
    }
    public class PlansVersionsCustomeDTO
    {
        public long PlanVersionId { get; set; }
        public List<PlanProductVersionCustomeDTO> planProductVersions { get; set; }

    }
    public class PlanProductVersionCustomeDTO
    {
        public long? ProductId { get; set; }
        public int? Quantity { get; set; }

    }
}
