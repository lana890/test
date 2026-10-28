using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Pages.Models; 
using System;
using System.Collections.Generic;
using System.Text;
using EskaCMS.Entities.Entity.BlogArchive;

using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.BlogArchive.Models
{
    public class PostsSearchVM
    {
        public string SearchValue { get; set; }
        public long SiteId { get; set; }
        public PostsSearchSortVM PostsSort { get; set; }
        public List<long> CategoriesIds { get; set; }
        public List<long> TagsIds { get; set; }
        public List<EStatus>? Statuses { get; set; }
        public string PublishDate { get; set; }
        public string PublishDateMonth { get; set; }
        public string Slug { get; set; }
        public int? PageSize { get; set; } = null;
        public int PageNo { get; set; }
        public long? CultureId { get; set; }
        public string CultureCode { get; set; }
        public long? AutherId { get; set; }
        public bool OnlyMyPosts { get; set; }

    } 
    public class PostsSearchSortVM
    {
        public PostsSearchSortFieldsEnum SortField { get; set; }
        public PostsSearchSortTypeEnum SortType { get; set; }

    }
    public enum PostsSearchSortFieldsEnum
    {
        Id = 1,
        Title = 2,
        StatusDate = 3,
        ExpiryDate = 4,
        CreationDate = 5,
        Description = 6,
        PublishDate=7,
    }
    public enum PostsSearchSortTypeEnum
    {
        asc = 1,
        desc = 2,
    }
    public class PostsListOutputVM<T>
    {
        public List<T> List { get; set; }
        public int TotalCount { get; set; }

    }
    public class PostsOutputVM
    {
        public long Id { get; set; }
        public long ParentId { get; set; }
        public string Title { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset? PublishDate { get; set; }
        public EStatus Status { get; set; }
        public DateTimeOffset? ExpirationDate { get; set; }
        public List<string> Categories { get; set; }
        public List<PostCulture> PostCultures { get; set; }
        public long CultureId { get; set; }
        public string Description { get; set; }
        public string ThumbnailUrl { get; set; }
        public bool IsLoginRequired { get; set; }
        public string Slug { get; set; }
        public long SiteId { get; set; }


    }

    public class PostCulture
    {
        public long Id { get; set; }
        public long ParentId { get; set; }
        public EStatus Status { get; set; }
        public string Title { get; set; }
        public  SitesCultures Culture { get; set; }
        public LoginPageDetails LoginPageDetails { get; set; }
        public bool IsLoginRequired { get; set; }
        public string Slug { get; set; }
        //public List<PostsCategories> PostsCategories { get; set; }
        public List<PostsCategoriesOutVM > PostsCategories { get; set; }
        public string Description { get; set; }
        public string Keywords { get; set; }


    }

    public class LoginPageVM
    {

        public string Name { get; set; }
        public long ParentPageId { get; set; }
    }

    public class PostsCategoriesOutVM
    {
        public long PostId { get; set; }
        public long CategoryId { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public virtual Category Category { get; set; }
    }

}
