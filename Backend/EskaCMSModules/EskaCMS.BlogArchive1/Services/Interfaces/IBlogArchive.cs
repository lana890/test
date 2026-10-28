
using EskaCMS.BlogArchive.Models;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.BlogArchive;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.BlogArchive.Services.Interfaces
{
    public interface IBlogArchive
    {
        Task<PostsListOutputVM<PostsViewModel>> GetPosts(PostsSearchVM search);
        Task<PostsViewModel> GetPostById(long PostId);
        Task<List<PostsViewModel>> GetPostByParentId(long ParentId);
        Task<PostsViewModel> GetPostBySlug(string Slug);
        Task<long> AddPost(PostsViewModel Post, bool isDuplicate = false);
        Task<bool> UpdatePostStatus(long PostId, long? ModifiedById, EStatus Status);
        Task<bool> UpdateRelativePostsStatus(long ParentPostId, long? ModifiedById, EStatus Status);

        Task<List<GetPostsCategories>> GetPostByCategoryId(long CategoryId, long CultureId);
        Task<long> AddEditPost( PostsViewModel EditPostObj);

        Task<long> DuplicatePost(long ParentId,long PostId,long UserId,long SiteId);
        Task<bool> UpdatePostsTemplate(string PostsIds, int TemplateId, long UserId, long siteId);
        Task<bool> UpdateStatusPosts(string PostsIds, long UserId, EStatus NewStatus);
        //Task<List<TempListsObj>> GetAllPostsTags(long SiteId);
        //Task<List<PostTagsVM>> GetAllPostsTags(long SiteId );
        Task<RolesViewModel> GetPostAssignedRoles(long Id, long SiteId);
        Task<bool> SavePostAssginedRoles(RolesViewModel vm);
        Task<PostsListOutputVM<PostsOutputVM>> GetPostsForAdmin(PostsSearchVM search);


		Task<List<Posts>> UpsertPostFirstStep(List<PostsViewModel> PostList, long UserId, long SiteId, bool isDuplicate = false);
        Task<bool> CompleteUpsertPost(List<CompleteUpsertPostVM> Posts);
        Task<PostsViewModel> GetPostByCultures(long parentId, long cultureId);
        Task<List<PostGroupByYearVM>> GetPostByYearAndMonth(FilterForMonthsViewModel viewModel);
        Task<bool> ChangeParentByCultureId(long siteId, long cultureId);
        Task<bool> SetLoginPage(SetPostLoginPageVM model);
        Task<List<PostsViewModel>> GetPostsByTagId(long siteId, long Id);
        Task<List<PostAuthorVM>> GetPostAuthorsByPostId(long siteId, long PostId);

        Task<long> UpsertPostForWeb(List<AddPostForWebVM> PostList, long UserId, long SiteId);
        Task<PostCreatedByInfo> GetCreatedByInfo(long PostId);

        Task DeleteCategoryDetails(long Id);
    }
}
