
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EskaCMS.Entities.Entity.Core
{

    public class SiteSettings : EntityBaseWithTypedId<string>
    {
        
        public SiteSettings(string id)
        {
           
            Id = id;
        }
        public long SiteId { get; set; }

        [StringLength(2000)]
        public string Value { get; set; }

        [StringLength(450)]
        public string Module { get; set; }

        public bool IsVisibleInCommonSettingPage { get; set; }

        public virtual Site Site { get; set; }
       

    }
}
