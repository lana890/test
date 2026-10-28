
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;

namespace EskaCMS.Entities.Entity.Core
{
    public partial class ThemeImportedFiles : EntityBase
    {
        public long ThemeId { get; set; }
        public Theme Theme { get; set; }


        public long MediaId { get; set; }
        public Media Media { get; set; }


    }

}

