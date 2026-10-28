using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Sites.Models
{
   
    public class SiteBaseDataViewModel
    {
        public long Id { get; set; }
        public long? OrganizationId { get; set; }
        public long? CoreBranchId { get; set; }
        public bool IsADAuthRequired { get; set; }
        public bool IsCoreIntegration { get; set; }
        public bool? EnableTwoFactorAuthentication { get; set; }
        public long CompanyId { get; set; }
    }
}
