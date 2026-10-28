using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Catalog
{
    public class ProductOptionValueDetails :EntityBase
    {
        [StringLength(450)]
        public string Value { get; set; }
        public long CultureId { get; set; }

        public EskaCMS.Entities.Entity.Core.SitesCultures Culture { get; set; }

        public long OptionValueId { get; set; }

        public ProductOptionValue OptionValue { get; set; }

    }
}
