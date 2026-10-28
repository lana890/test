using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Text;
using EskaCMS.Entities.Entity.General;

namespace EskaCMS.General.ViewModels
{
    public class LookupVM
    {
        public long id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string DefultValue { get; set; }
        public bool IsEditable { get; set; }
        public bool EnableDataSecurity { get; set; }
        public long? SiteId { get; set; }
        public long? ParentId { get; set; }
        public long? ParentSiteId { get; set; }
        public Entities.Enum.GeneralEnums.EStatus EStatus { get; set; }
        public Lookup Parent { get; set; }
        public List<Lookup> Children { get; set; }
        public long TotalChildren { get; set; }
        public string Code { get;  set; }
    }
    public class lookupViewVM
    {
        public long Id { get; set; }
        public long? ParentId { get; set; }
        public long TotalChildren { get; set; }

        public string Name { get; set; }
        public List<Lookup> Children { get; set; }
        public List<LookupValues> lookupvalues { get; set; }
        public string Code { get;  set; }
    }
    public class LookupParent
    {
        public long? Id { get; set; }
        public string  Name { get; set; }
        public string Code { get;  set; }
    }
}

