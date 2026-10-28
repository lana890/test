using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Catalog;
using System.Collections.Generic;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.UserCatalog.Dtos
{
    public class PlanProductVersionCreate
    {
        public long PlanVersionId { get; set; }
        public long ProductId { get; set; }
        public int Quantity { get; set; }
        public EStatus Status { get; set; }
    }
    public class PlanProductVersionUpdate
    {
        public long Id { get; set; }
        public long? PlanVersionId { get; set; }
        public long? ProductId { get; set; }
        public int? Quantity { get; set; }
        public int? Status { get; set; }

    }

    public class PlanProductVersionOutput
    {
        public long Id { get; set; }
        public long PlanVersionId { get; set; }
        public PlansVersionsOutput PlanVersion { get; set; }
        public long ProductId { get; set; }
        public Product Product { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public EStatus Status { get; set; }
        public decimal Price { get; set; }
        public string Description { get;  set; }
    }


    public class PlanProductVersionVM
    {
        public long Id { get; set; }
        public long PlanVersionId { get; set; }
        public List<PlanProductVM> ProductCultureDetails { get; set; }
        public int Quantity { get; set; }
        public EStatus Status { get; set; }
        public decimal Price { get; set; }

    }
}
