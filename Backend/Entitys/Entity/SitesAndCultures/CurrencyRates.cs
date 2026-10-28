using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EskaCMS.Infrastructure.Models;


namespace EskaCMS.Entities.Entity.Core
{
    public class CurrencyRates : EntityBase
    {

        
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(250)]
        public string Name { get; set; }
        public bool IsDefaultRate { get; set; }
        public bool IsDeleted { get; set; }

        public decimal Rate { get; set; }
     
        public long SiteId { get; set; }

        public string CultureCode { get; set; }

    }
}
