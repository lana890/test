

using EskaCMS.Infrastructure.Models;
using EskaCMS.Entities.Entity.Catalog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Pages;
using EskaCMS.Entities.Entity.BlogArchive;

namespace EskaCMS.Entities.Entity.Menus
{
    public partial class Menu : EntityBase
    {
        public Menu()
        {
            CreationDate = DateTimeOffset.Now;
            Params = new List<Params>();
        }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }


        public EskaCMS.Entities.Enum.GeneralEnums.EMenuTypes MenuType { get; set; }
        //    public string MenuTypeURL { get; set; }

        public long? ThumbnailId { get; set; }
        public string Title { get; set; }
        public virtual Media Thumbnail { get; set; }

        public string ExternalSlug { get; set; }
        public long? PageId { get; set; }

        public virtual Page Page { get; set; }

        public long? PostId { get; set; }
        public virtual Posts Post { get; set; }
        public long? ProductId { get; set; }
        public virtual Product Product { get; set; }

        public long? BrandId { get; set; }
        public virtual Brand Brand { get; set; }
        public long? Order { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }

        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }

        public long SiteId { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }


        public long? ParentId { get; set; }
        public Menu Parent { get; set; }

   
        public long? TemplateId { get; set; }
        public virtual PageTemplates Template { get; set; }

        public IList<Params> Params { get; set; } 

        public bool IsLoginRequired { get; set; }

        public ICollection<MenusRoles> MenuRoles { get; set; }
        //   public virtual MenuSettings MenuSettings { get; set; }


        public long MenuGroupId { get; set; }
        public virtual MenuGroup MenuGroup { get; set; }

        public long? CategoryId { get; set; }
        public Category Category { get; set; }


        [NotMapped]
        public long SlugTargetId { get; set; }
    }
}
