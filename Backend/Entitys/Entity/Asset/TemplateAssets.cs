
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Infrastructure.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Core

{

    public partial class TemplateAssets : EntityBase
    {

        public int Order { get; set; }

        public long AssetId { get; set; }

        public Assets Asset { get; set; }

        public long TemplateId { get; set; }

        public PageTemplates Template { get; set; }

        public EStatus Status { get; set; }

    }
}
