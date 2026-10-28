using EskaCMS.Core.ViewModels;
using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Theming.ThemeVM
{
    public class ThemeVM
    {
        public string Classes { get; set; }
        public string Fonts { get; set; }
        public string Styles { get; set; }
        public string Scripts { get; set; }
        public List<ImportedFilesVM> ImportedFiles { get; set; }
        public string Colors { get; set; }

    }
}
