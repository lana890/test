
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EskaCMS.Entities.Entity.Pages;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;

namespace EskaCMS.Entities.Entity.BlogArchive
{
   public partial class Posts : EntityBase 
    {
        //public Posts()
        //{
        //    PostsTags = new HashSet<PostsTags>();
        //}
         
        public long PostTemplateId { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(1000)]
        public string Title { get; set; }

        public DateTimeOffset CreationDate { get; set; }

        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }
        public DateTimeOffset StatusDate { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }

        public string PostContent { get; set; }

        public DateTimeOffset? PublishDate { get; set; }

        [StringLength(10000)]
        public string Description { get; set; }

        [StringLength(10000)]
        public string ShortDescription { get; set; }

        public DateTimeOffset? ExpiryDate { get; set; }
        public string Keywords { get; set; }
        public string metaDescription { get; set; }
        public string addtionalMetaTags { get; set; }
        public short IsPrivate { get; set; }
        public string Settings { get; set; }

        public bool IsLoginRequired { get; set; }



        // Relateions
        public long CultureId { get; set; }
        public virtual SitesCultures Culture  { get; set; }

        [ForeignKey(nameof(LoginPage))]
        public long? LoginPageId { get; set; }
        public virtual Page LoginPage { get; set; }
        public long? ParentId { get; set; }
        public virtual Posts Parent { get; set; }
        public IList<Posts> Children { get; set; }


        public long SlugInfoId { get; set; }
        public SlugInfo SlugInfo
        {
            get { return _SlugInfo; }
            set
            {

                _SlugInfo = value;
                Slug = value == null ? string.Empty : value.Slug;
            }
        }

        [NotMapped]
        private SlugInfo _SlugInfo { get; set; }

        [NotMapped]
        public string Slug { get; set; }

        public virtual ICollection<PostsCategories> PostsCategories { get; set; }
        public virtual IList<PostRoles> PostRoles { get; set; }
        //  public virtual ICollection<PostsTags> PostsTags { get; set; }
        public virtual ICollection<PostsAuther> PostsAuthers { get; set; }

        public long? ThumbnailId { get; set; }
        public virtual Media Thumbnail { get; set; }

        public virtual ICollection<PostMedias> PostMedias { get; set; }
        public string RelatedCategoriesSettings { get; set; }

        public virtual PageTemplates PostTemplate { get; set; }


        public long SiteId { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }

        public long CreatedById { get; set; }
        public virtual User CreatedBy { get; set; }

        public long? ModifiedById { get; set; }
        public virtual User ModifiedBy { get; set; }


    }
}
