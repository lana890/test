using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Sites.Models
{
   
    public class CreateSitePreferanceViewModel
    {
        public string DateFormate { get; set; }
        public string TimeFormate { get; set; }
        public string FontSize { get; set; }
        public long? UserId { get; set; }
        public long? ThemeId { get; set; }

    }


    public class GetSitePreferanceVM
    {
        public string DateFormate { get; set; }
        public string TimeFormate { get; set; }
        public string FontSize { get; set; }
        public long? ThemeId { get; set; }
        public ThemeVM ThemeDetails { get; set; }
        public long Id { get; set; }
    }

    public class UpdateSitePreferanceVM
    {
        public string DateFormate { get; set; }
        public string TimeFormate { get; set; }
        public string FontSize { get; set; }
        public long? ThemeId { get; set; }
    }

    public class ThemerPreferanceVM
    {
        public string Name { get; set; }
        public string Colors { get; set; }
        public string Classes { get; set; }
        public string Fonts { get; set; }
        public string StyleUrl { get; set; }
        public string Scripts { get; set; }
        public List<ImportedFilesVM> ImportFiles { get; set; }

    }
}
