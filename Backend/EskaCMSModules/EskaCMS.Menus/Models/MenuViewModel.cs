using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Models;
using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Menus.Models
{
  
        public class MenuViewModel
    {
      
        public long Id { get; set; }
        public string Name { get; set; } 
        public string Icon { get; set; }
        public EMenuTypes MenuType { get; set; } 

        public long? PageId { get; set; }
        public long? PostId { get; set; }
        public long SlugTargetId { get; set; }

        public long? ProductId { get; set; }
        public long? CategoryId { get; set; } 

        public bool Expanded { get; set; } = true;

        public string Title { get; set; } 
        public string Description { get; set; } 
        public long? ThumbnailId { get; set; }
        public string ThumbnailURL { get; set; }
        public string ExternalSlug { get; set; }
        public string Slug{ get; set; }  // pageSlug
       // public string Content { get; set; }
        public long SiteId { get; set; }
        public EStatus Status { get; set; } // status 
        public long CategoriesId { get; set; }
        public long? ParentId { get; set; } 
       // public List<Menu> Parent { get; set; }
        public long? TemplateId { get; set; }
        public long? Order { get; set; }
        //  public string AddtionalMetaTags { get; set; }

        
        public List<MenuViewModel> Childs { get; set; }


       

    public CategoryViewModel category = new CategoryViewModel();
        public long UserId { get; set; }

        public long MenuGroupId { get; set; }
        public bool IsActive { get; set; }
        public long? BrandId { get;  set; }

        public List<ParamsViewModel>? Params { get; set; } = new List<ParamsViewModel>();
    }


    public class MenuBreadCrumbVM
    {
        public long Id { get; set; }
        public string Name { get; set; } 
        public string Icon { get; set; }

        public string Title { get; set; } // PageName
        public long? ThumbnailId { get; set; }
        public string ThumbnailURL { get; set; }
        public EMenuTypes MenuType { get; set; }
        public string Slug { get; set; }  // pageSlug
                                          // public string Content { get; set; }
        public long? ParentId { get; set; }
        public long MenuGroupId { get; set; }

    }


}
