using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Address
{
    public class StateOrProvinceDetails :EntityBase
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }
        public long CultureId { get; set; }
        public SitesCultures Culture { get; set; }
        public long StateOrProvinceId { get; set; }
        public StateOrProvince StateOrProvince { get; set; }
    }
}
