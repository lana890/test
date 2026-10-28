using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.ViewModels
{
    public class MediaDetailsWithOrderVM
    {

        public long Id { get; set; }
        public string Title { get; set; }
        
        public string AltText { get; set; }

        public string Description { get; set; }


        public long CreatedById { get; set; }

        public long? ModifiedById { get; set; }

        public long MediaId { get; set; }
        public Media Media { get; set; }
        public long CultureId { get; set; }
        public virtual SitesCultures Culture { get; set; }

        public long? Order { get; set; }

    }
}
