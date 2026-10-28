using EskaCMS.Entities.Entity;
using EskaCMS.Pages.Models;
using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Core.Areas.Core.ViewModels.Tags;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace EskaCMS.BlogArchive.Models
{

    public class TempListsObj
    {
        public long id { get; set; }
        public string label { get; set; }
        public string Name { get; set; }
		public string CategoryId { get; set; }
	}
    public class PostsViewModel
    {
        public long Id { get; set; }
        public long? TemplateId { get; set; }
        public PageTemplateVM Template { get; set; }
        public List<TempListsObj> CategoryList { get; set; }
        public List<TempListsObj> TagList { get; set; }
        public PostTemplate PostTemplate { get; set; }
        public long? ThumbnailId { get; set; }

        public List<PostAuthorVM> PostsAuthor { get; set; }
        public long ParentId { get; set; }

        public long CultureId { get; set; }
        public string CultureCode { get; set; }
        public string Title { get; set; }

        public string Description { get; set; }
        public string ShortDescription { get; set; }

        public DateTimeOffset? ExpiryDate { get; set; }
        public DateTimeOffset? PublishDate { get; set; }

        public short IsPrivate { get; set; }

        public string PostContent { get; set; }

        public string Slug { get; set; }

        public long CreatedById { get; set; }

        public DateTimeOffset CreationDate { get; set; }

        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }

        public EStatus Status { get; set; }

        public string StatusDesc { get; set; }
        public string ThumbnailURL { get; set; }
        public DateTimeOffset StatusDate { get; set; }

        public long SiteId { get; set; }
        public long UserId { get; set; }

        public string SiteName { get; set; }
        public List<long> Media { get; set; }
        public List<PostMediaWithURLViewModel> MediaURLs { get; set; }
        public string Settings { get; set; }

        public List<PostCategoriesVM> PostsCategories { get; set; }
        public List<long> PostsRolesIds { get; set; }
        public List<GetTagsViewModel> PostsTags { get; set; }
        public string Keywords { get; set; }
        public string metaDescription { get; set; }
        public string addtionalMetaTags { get; set; }
        public List<PostCategoriesVM> Categories { get; set; }

        public string RelatedCategoriesSettings { get; set; }
        public List<PostAuthorVM> PostAuthorIds { get; set; }
        public List<long> AuthorIds { get; set; }

        public LoginPageDetails LoginPage { get; set; }
        public bool IsLoginRequired { get; set; }
        public long? LoginPageId { get; set; }


    }



  




    public class AddPostForWebVM
    {
        public long Id { get; set; }
        //public long? FolderId { get; set; }
        public long? TemplateId { get; set; }
        public long? ThumbnailId { get; set; }
        //public IFormFile Thumbnail { get; set; }
        //public IFormFileCollection Images { get; set; }  
        public long ParentId { get; set; }

        public long CultureId { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }
        public string ShortDescription { get; set; }
        public EStatus Status { get; set; }

        public DateTimeOffset? ExpiryDate { get; set; }
        public DateTimeOffset? PublishDate { get; set; }

        public short IsPrivate { get; set; }

        public string PostContent { get; set; }

        public string Slug { get; set; }
        [Required]
        public List<PostMediaWithURLViewModel> MediaURLs { get; set; }
        public string Settings { get; set; }

        public List<long>? PostsRolesIds { get; set; }
        public string Keywords { get; set; }
        public string metaDescription { get; set; }
        public string addtionalMetaTags { get; set; }
        public List<PostCategoriesVM>? PostsCategories { get; set; }
        public string RelatedCategoriesSettings { get; set; }
        public List<long>? AuthorIds { get; set; }

        public IList<TagsViewModel>? Tags { get; set; }


    }

    public class FilterForMonthsViewModel
    {
        public string? startYear { get; set; }
        public string? Month { get; set; }
        public List<long>? CategoriesIds { get; set; } = new List<long>();


    }


    public class PostSettingsVM
    {
        public bool isExternal { get; set; }
        public string link { get; set; }
        public bool newTabLink { get; set; }
    }
    public class CompleteUpsertPostVM
    {
        public long Id { get; set; }

        public string Keywords { get; set; }
        public string metaDescription { get; set; }
        public string addtionalMetaTags { get; set; }

        public string RelatedCategoriesSettings { get; set; }
        public string PostContent { get; set; }
        public string Description { get; set; }
        public string ShortDescription { get; set; }
        public List<PostTagsVM> Tags { get; set; }
    }

    public class RolesViewModel
    {
        public long Id { get; set; }
        public List<Role> assingedRoles { get; set; }
        public List<Role> unassingedRoles { get; set; }
        public long UserId { get; set; }

    }



}
