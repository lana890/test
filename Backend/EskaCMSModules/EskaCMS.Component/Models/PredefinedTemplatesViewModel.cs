using EskaCMS.Infrastructure.Web.SmartTable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Components.Models
{
    public class PredefinedTemplatesViewModel
    {
        public long? Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string SettingsType { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Settings { get; set; }
    }

    public class PredefinedTemplatesSearchTableParam<T>
    {
        public Pagination Pagination { get; set; }

        public T Search { get; set; }


    }

    public class SearchPredefinedTemplatesViewModel
    {
        public long? Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string SettingsType { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Settings { get; set; }
        public string FullSearch { get; set; }
    }
}
