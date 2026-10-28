using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels
{
  public  class AdditionCartInfoVM
    {
        public long PlanId { get; set; }
        public CartTypes CartType { get; set; }
        public long? NextPlanId { get; set; }
        public bool? IsPlanTrail { get; set; }
        public bool? IsOneTimeService { get; set; }

    }

    public class DeliveryOption {

        public bool IsAramix { get; set; }
        public bool InsideJordan { get; set; }

        public long CartId { get; set; }


    }


}
