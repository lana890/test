using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Localization
{
    public class ResourcesKey:EntityBase
    {

        public string Key { get; set; }
        public long SiteId { get; set; }
        public EskaCMS.Entities.Entity.Core.Site Site { get; set; }

        // null slug means that this key is global among the Slugs
      

        // null => global among the Components
        public string Component { get; set;}

        // no Group with Component Original key
        // no Group with null Component Generic  
        public string GroupId{ get; set;}

        public bool IsShared { get; set; }

       


        [NotMapped]
        public EskaCMS.Entities.Enum.GeneralEnums.EResouseScope Scope { get {
                if (!string.IsNullOrEmpty(this.GroupId))
                {
                    return EskaCMS.Entities.Enum.GeneralEnums.EResouseScope.AmongGroup;
                }
                else if (!this.IsShared)
                {
                    return EskaCMS.Entities.Enum.GeneralEnums.EResouseScope.AmongSlug;
                }
                else
                {
                    return EskaCMS.Entities.Enum.GeneralEnums.EResouseScope.AmongSite;
                }
            } }
        public List<ResourcesValue> ResourcesValues { get; set; }


    }
}
