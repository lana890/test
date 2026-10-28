using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Components.Models
{

    public class ComponentViewModel 
    {
        public int Id { get; set; }
        public string TokenId { get; set; }
        public string Name { get; set; }
        public string Name2 { get; set; }
        public string Title { get; set; }
        public string DataKey { get; set; }
        public long? Type { get; set; }
        public long? CategoryId { get; set; }
        public EStatus Status { get; set; }
        public DateTimeOffset? StatusDate { get; set; }
        public string Settings { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long SiteId { get; set; }
 
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public long UserId { get; set; }

        public CategoryViewModel category { get; set; }

    }

    public class CategoryComponentViewModel
    {
        public long Id { get; set; }
        public long? ParentId { get; set; }
        public bool IncludeInMenu { get; set; }
        public int DisplayOrder { get; set; }
        public long CategoryTypeId { get; set; }

        public MediaViewModel media { get; set; }=new MediaViewModel();
        public EStatus Status { get; set; }

        public string Slug { get; set; }

        public string Name { get; set; }
    }

    //public class CategoryDetailsComponentViewModel

    //{

    //    public string Slug { get; set; }
    //    public long? CategoryTemplateId { get; set; }

    //    public ECategoryTypes CategoryTypeId { get; set; }
    //    public string MetaTitle { get; set; }

    //    public string MetaKeywords { get; set; }

    //    public string MetaDescription { get; set; }

    //    public string Description { get; set; }






    //    public string CategoryType { get; set; }

    //    public string CultureCode { get; set; }
    //    public long? CultureId { get; set; }
    //}

    public class MediaViewModel

    {
        public long? Id { get; set; }
        public string PublicUrl { get; set; }
        public string FileName { get; set; }
    }
}
