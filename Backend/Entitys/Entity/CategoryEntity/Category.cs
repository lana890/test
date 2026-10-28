
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Core
{
    public partial class Category : EntityBase
    {

        public Category()
        {
            Options = new HashSet<Option>();
            Children = new List<Category>();
        }
        public Category(long id)
        {
            Id = id;
        }
    
        public long CreatedById { get; set; }

        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }


        public long? ThumbnailImageId { get; set; }
        public virtual Media ThumbnailImage { get; set; }

        public long? CoverImageId { get; set; }
        public virtual Media CoverImage { get; set; }

        public virtual User CreatedBy { get; set; }
        public virtual User ModifiedBy { get; set; }

        public int DisplayOrder { get; set; }
        public DateTimeOffset StatusDate { get; set; }
        public bool IncludeInMenu { get; set; }

        public long? ParentId { get; set; }
        public Category Parent { get; set; }
        public DateTimeOffset? PublishDate { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }
        public IList<Category> Children { get; protected set; }

        public long CategoryTypeId { get; set; }
        public virtual CategoryTypes CategoryType { get; set; }

        public virtual Site Site { get; set; }
        public long? SiteId { get; set; }
        public ICollection<Option> Options { get; set; }

        public IList<CategoryDetails> CategoryDetails { get; set; }
        public IList<ProductCategory> ProductCategories { get; set; }
        public IList<CategoryWarehouse>  CategoryWarehouses { get; set; }
        public EStatus Status { get; set; }
        public bool IsPromotional { get; set; }
        public bool IsFeatured { get; set; }
    }
}
