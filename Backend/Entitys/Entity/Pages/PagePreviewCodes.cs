using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Pages
{
    public class PagePreviewCodes : EntityBase
    {
        public string Code { get; set; }
        public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.Now;
        [ForeignKey(nameof(PageVersion))]
        public long PageVersionId { get; set; }
    
        public PageVersions PageVersion { get; set; }

    }
}
