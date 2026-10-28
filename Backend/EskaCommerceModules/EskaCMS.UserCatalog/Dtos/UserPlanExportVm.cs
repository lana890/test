using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.UserCatalog.Dtos
{
    public class UserPlanExportVm
    {
        public long Id { get; set; }
        public string CustomerName { get; set; }
        public DateTimeOffset ActivationDate { get; set; }
        public EUserPlansStatus CurrentStatus { get; set; }
        public DateTimeOffset CurrentStatusDate { get; set; }
        public EUserPlansStatus NextStatus { get; set; }
        public DateTimeOffset NextStatusDate { get; set; }
        public string OrderTotal { get; set; }


    }

    public class GatewayApiResponse
    {
        public string Status { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public string Data { get; set; }
    }
}
