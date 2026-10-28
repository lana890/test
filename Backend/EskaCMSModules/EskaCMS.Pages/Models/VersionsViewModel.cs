using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Pages.Models
{
 
    public class VersionsViewModel
    {
        public DateTimeOffset CreationDate { get; set; }
        public long Id { get; set; }
        public long CreatedById { get; set; }
        public long VersionId { get; set; }
        public string CreatedByName { get; set; }
        public string Alias { get; set; }
        public bool IsPublished { get; set; }
        

    }
}
