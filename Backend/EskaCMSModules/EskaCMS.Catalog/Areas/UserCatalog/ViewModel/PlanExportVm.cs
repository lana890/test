using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.UserCatalog.Dtos
{
    public class PlanExportVm
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsTopPicked { get; set; }
        public PlanTypes PlanType { get; set; }
        public RecurringTypes RecurringType { get; set; }
        public string CreatedBy { get; set; }
        public bool IsCustomPlan { get; set; }
        public DateTimeOffset CreationDate { get; set; }
    }
}
