using EskaCMS.Core.Entities;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EskaCMS.Core.Models
{
    public class AppSetting : EntityBaseWithTypedId<string>
    {
        public AppSetting(string id)
        {
            Id = id;
        }

        [StringLength(450)]
        public string Value { get; set; }

        [StringLength(450)]
        public string Module { get; set; }

        public bool IsVisibleInCommonSettingPage { get; set; }

        public long SiteId { get; set; }

        public virtual Site Site { get; set; }

    }

}
