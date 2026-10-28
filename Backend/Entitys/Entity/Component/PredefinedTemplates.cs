using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Component
{
    public class PredefinedTemplates : EntityBase
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string SettingsType { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Settings { get; set; }

    }
}
