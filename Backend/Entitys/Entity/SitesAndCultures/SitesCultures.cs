using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using EskaCMS.Infrastructure.Models;


namespace EskaCMS.Entities.Entity.Core
{
    public class SitesCultures : EntityBase
    {
        public long SiteId { get; set; }
        
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public virtual Site Site { get; set; }

        public string CultureId { get; set; }
        public virtual Culture Culture { get; set; }

        public string CulturePrefix { get; set; }

        public EskaCMS.Entities.Enum.GeneralEnums.CultureType CultureType { get; set; }
        public string? CssStyle { get; set; }

    }
}
