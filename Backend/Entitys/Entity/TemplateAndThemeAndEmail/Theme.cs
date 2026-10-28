

using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace EskaCMS.Entities.Entity.Core
{
    public partial class Theme : EntityBase
    {
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public virtual User CreatedBy { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public virtual User ModifiedBy { get; set; }


        public string Name { get; set; }

        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }
        public DateTimeOffset StatusDate { get; set; }
        public bool IsDefault { get; set; }

        public string Colors { get; set; }
        public string Classes { get; set; }
        public string Fonts { get; set; }
        public string Styles { get; set; }
        public string Scripts { get; set; }

        [NotMapped]
        public List<ImportedFilesVM> ImportFilesList { get; set; } // for Imported files Data

        public IList<ThemeImportedFiles> ImportedFiles { get; set; } // forign Key

        public long SiteId { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }

    }
}
