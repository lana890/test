using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace EskaCMS.Entities.Entity.Pages
{
    public  class PageComponentSettings :EntityBase
    {
        public string ComponentName { get; set; }

        public string Settings { get; set; }
        public string ComponentSettingsId { get; set; }
        public bool IsLazyUi { get; set; }
        public string? Alias { get; set; }
        public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.Now;
        public long PageVersionId { get; set; }
        public long SiteId { get; set; }
        public PageVersions PageVersion { get; set; }
        public Site Site { get; set; }






    }
}
