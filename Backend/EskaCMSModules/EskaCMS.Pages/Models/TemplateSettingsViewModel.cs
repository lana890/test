using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Pages.Models
{
 
    public class PageTemplateVM
    {
        public string Name { get; set; }

        public string Description { get; set; }
        public string AddtionalMetaTags { get; set; }

        public long Type { get; set; }
        public TemplateVersionVM CurrentVersion { get; set; }
        public List<GenericAssetsViewModel>  genericAssets { get; set; } =new List<GenericAssetsViewModel>();


    }
    public class GenericAssetsViewModel

    {
        public EAssetsType Type { get; set; }
        public string Extensions { get; set; }

        public string Url { get; set; }

    }
    public class PostTemplate
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    public class AssetTemplateViewModel
    {
        public  int Order { get; set; }

        public EAssetsType Type { get; set; }
        public string Extensions { get; set; }
        public long SiteId { get; set; }
        public string Name { get; set; }
    }
}
