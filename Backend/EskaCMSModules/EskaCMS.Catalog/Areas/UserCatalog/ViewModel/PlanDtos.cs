using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Tax;
using EskaCMS.Infrastructure.Models;
using EskaCMS.Infrastructure.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.UserCatalog.Dtos
{
    public class PlanCreate
    {
       
        public List<PlanCultureDetailsVm> PlanDetails { get; set; }

        public List<long> CategoryIds { get; set; }
        
        public PlanTypes PlanType { get; set; }
        public string Image { get; set; }
        public EStatus Status { get; set; }

        public bool IsFeatured { get; set; }
        public bool IsTopPicked { get; set; }
        public bool IsCustomPlan { get; set; }
        public int Order { get; set; }
        public long? NextPlanId { get; set; }

        public RecurringTypes RecurringType { get; set; }


    }
    public class PlanUpdate
    {
        public long Id { get; set; }
        public PlanTypes PlanType { get; set; }
        public EStatus Status { get; set; }
        public string Image { get; set; }
        public List<PlanCultureDetailsVm> PlanDetails{ get; set; }
        public bool IsFeatured { get; set; }
        public bool IsTopPicked { get; set; }
        public bool IsCustomPlan { get; set; }
        public int Order { get; set; }
        public RecurringTypes RecurringType { get; set; }
        public long? NextPlanId { get; set; }

        public List<long> CategoryIds { get; set; }

    }
    public class PlanOutput
    {

        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsParent { get; set; }
        public long SiteId { get; set; }
        public string Image { get; set; }
        public PlanTypes PlanType { get; set; }
        public RecurringTypes RecurringType { get; set; }
        public EStatus Status { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public string ShortDescription { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsTopPicked { get; set; }
        public bool IsCustomPlan { get; set; }
        public bool IsSubscribed { get; set; }
        public bool IsChildSubscribed { get; set; }
        public decimal Price { get; set; }
        public long? NextPlanId { get; set; }

        public IList<PlansVersionsOutput> Versions { get; set; }
        public PlansVersionsOutput LatestVersion { get; set; }
        public IList<PlanProductVersionOutput> PlanProducts { get; set; }
        public decimal  DicountValue { get; set; }
        public DiscountTypes DiscountType { get; set; }    
        public long DiscountId { get; set; }
        public int Order { get; set; }

        public decimal YouSave { get; set; }
        public TaxClass TaxClass { get; set; }


    }

    public class PlanDetailsVM
    {

        public List<PlanCultureDetailsVm> PlanCultureDetails { get; set; }
        public long Id { get; set; }
        public string Image { get; set; }
        public PlanTypes PlanType { get; set; }
        public RecurringTypes RecurringType { get; set; }
        public EStatus Status { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsTopPicked { get; set; }
        public bool IsCustomPlan { get; set; }
        public List<PlansVersionsVM> Versions { get; set; }
        public int Order { get; set; }
        public List<long> CaregoryIds { get; set; }



    }

    public class PlanListVM
    {

        public long Id { get; set; }
        public List<PlanCultureDetailsVm> PlanDetails { get; set; }
        
        
        public bool IsFeatured { get; set; }
        public bool IsTopPicked { get; set; }
        public EStatus Status { get; set; }
        public bool IsCustomPlan { get; set; }

        public PlanTypes PlanType { get; set; }
        public RecurringTypes RecurringType { get; set; }

    }




    public class PlanSearchrequestDto
    {
        public long CategoryId { get; set; }
        public string SearchValue { get; set; }
        public string PageSize { get; set; }
        public string PageNumber { get; set; }
        public PlanTypes PlanType { get; set; }

    }

    public class PlanResponseDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public PlanTypes PlanType { get; set; }
        public RecurringTypes RecurringType { get; set; }
        public EStatus Status { get; set; }

        public string ShortDescription { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsTopPicked { get; set; }
        public bool IsCustomPlan { get; set; }
        public bool IsSubscribed { get; set; }
        public decimal Price { get; set; }
        public PlansVersionsOutput Versions { get; set; }
        public decimal DiscountValue { get; set; }
        public int Order { get; set; }
        public bool ExistCustomePlanByUser { get; set; }

        public TaxClass TaxClass { get; set; }


    }

}
