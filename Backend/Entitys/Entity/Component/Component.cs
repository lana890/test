

using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EskaCMS.Entities.Entity.Core;
namespace EskaCMS.Entities.Entity.Components
{
    public  class Component :EntityBase
    {
       // public int Id { get; set; }
        public string TokenId { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
       
        public string Name { get; set; }
        
        public string Name2 { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
     
        public string Title { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
      
        public string DataKey { get; set; }

        public EskaCMS.Entities.Enum.GeneralEnums.EComponentType Type { get; set; }
        public long CategoryId { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
       
        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public DateTimeOffset StatusDate { get; set; }
   
        public long CreatedById { get; set; }
    
        public DateTimeOffset CreationDate { get; set; }
        [Required(ErrorMessage = "The {0} field is required.")]
        public long SiteId { get; set; }

        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }

        public virtual Category Category { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }

        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }
        public string Settings { get; set; }

        public long? CultureId { get; set; }
        public virtual SitesCultures Culture { get; set; }
    }
}
