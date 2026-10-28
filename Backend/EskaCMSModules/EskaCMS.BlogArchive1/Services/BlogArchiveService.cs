
using EskaCMS.BlogArchive.Models;
using EskaCMS.BlogArchive.Services.Interfaces;
using EskaCMS.Core.Data;
using EskaCMS.Core.Entities;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interface;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Pages.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.BlogArchive;
using EskaCMS.Media.Models;
using EskaCMS.Media.Services.Interfaces;
using EskaCMS.Entities.Entity;

using static EskaCMS.Entities.Enum.GeneralEnums;
using Microsoft.Extensions.Hosting;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Core.Areas.Core.ViewModels.Tags;
using Microsoft.AspNetCore.Http;
using System.Transactions;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.DataAccess.Data;
using EskaCMS.Infrastructure.Modules.Repositories;
using EskaCMS.Infrastructure.Models.ElasticVM;
using EskaCMS.Core.Cache;
using System.Diagnostics.Eventing.Reader;
using EskaCMS.Entities.Entity.Shared;
using EskaCMS.Entities.Entity.Catalog;
using Microsoft.Extensions.Configuration;
//using EskaCMS.Search.Areas.Search.ViewModels;

namespace EskaCMS.BlogArchive.Services
{
    public class BlogArchiveService : IBlogArchive
    {

        private readonly IRepository<Posts> _PostRepository;
        private readonly IRepository<PostsCategories> _PostsCategoriesRepository;
        private readonly IRepository<PostMedias> _PostMediasRepository;
        private readonly IRepository<EskaCMS.Entities.Entity.Core.Media> _MediaRepository;
        private readonly IRepository<Tags> _TagsRepository;
        private readonly IRepository<EntityTags> _EntityTagsRepository;
        private readonly IRepository<Category> categoryRepository;
        private readonly IRepository<CategoryDetails> categoryDetailsRepository;
        private readonly RoleManager<Role> _roleManager;
        private readonly IRepository<PostRoles> _postRoleRepository;
        private readonly IWorkContext _workContext;
        private readonly EskaDCMSDbContext _context;
        public readonly ISlugServices _SlugServices;
        private readonly IRepository<PostsAuther> _PostAuther;
        private readonly ITagsServices _TagsService;
        private readonly Media.Services.Interfaces.IMediaService _mediaService;
        private readonly ISearchRepository<NaviagtableSearchModel> _elasticRepo;
        private readonly ICacheService cacheService;
        private readonly IClientCacheService clientCacheService;
        private readonly IConfiguration _config;
        private readonly ISearchRepository<EskaCMS.Infrastructure.Models.MeiliSearchVM.NaviagtableSearchModel> _meiliRepo;

        public BlogArchiveService(RoleManager<Role> roleManager, IRepository<PostRoles> postRoleRepository,
            IRepository<Posts> postRepository,
                                IRepository<PostsCategories> PostsCategoriesRepository,
                                IRepository<PostMedias> PostMediasRepository,
                                IRepository<Tags> TagsRepository, IRepository<Category> CategoryRepository,
            IRepository<CategoryDetails> CategoryDetailsRepository,
                                IRepository<EskaCMS.Entities.Entity.Core.Media> MediaRepository,
                                IWorkContext workContext,
                                ISlugServices SlugServices,
                                ITagsServices TagsService,
                                IRepository<PostsAuther> PostAuther,
                                 EskaDCMSDbContext context,
                                 Media.Services.Interfaces.IMediaService mediaService,
                                 ISearchRepository<NaviagtableSearchModel> elasticRepo,
                                 ISearchRepository<EskaCMS.Infrastructure.Models.MeiliSearchVM.NaviagtableSearchModel> meiliRepo,
                                 ICacheService cacheService,
                                 IClientCacheService clientCacheService,
                                 IRepository<EntityTags> EntityTagsRepository,
                                 IConfiguration config

            )
        {
            _PostRepository = postRepository;

            _PostsCategoriesRepository = PostsCategoriesRepository;
            _TagsRepository = TagsRepository;
            categoryRepository = CategoryRepository;
            categoryDetailsRepository = CategoryDetailsRepository;
            _PostMediasRepository = PostMediasRepository;
            _MediaRepository = MediaRepository;
            _roleManager = roleManager;
            _postRoleRepository = postRoleRepository;
            _workContext = workContext;
            _context = context;
            _SlugServices = SlugServices;
            _PostAuther = PostAuther;
            _TagsService = TagsService;
            _mediaService = mediaService;
            _elasticRepo = elasticRepo;
            this.cacheService = cacheService;
            this.clientCacheService = clientCacheService;
            _EntityTagsRepository = EntityTagsRepository;
            _meiliRepo = meiliRepo;
            _config = config;
        }



        // this region needs to be deleted after testing the new one
        #region Old Service


        public async Task<long> AddEditPost(PostsViewModel EditPostObj)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
            await using var DBTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                    //if (EditPostObj.Id == 0)
                    //    throw new Exception("Post Id is missing");
                    var CreatedById = await _workContext.GetCurrentUserId();

                    var ObjPost = await _PostRepository.Query()
                        .Where(x => x.Id == EditPostObj.Id)
                        .Include(x => x.SlugInfo)
                        .FirstOrDefaultAsync();
                    var ObjPosts = new Posts();

                    if (EditPostObj.Id == 0)
                    {
                    Core.Models.SlugManagementVm slugManagementVm = new()
                    {
                        Id = ObjPost.SlugInfoId,
                        Slug = EditPostObj.Slug,
                        CultureId = EditPostObj.CultureId,
                        Entity = EEntity.Post,
                        SiteId = EditPostObj.SiteId

                    };
                        ObjPosts.PostTemplateId = EditPostObj.TemplateId.Value;
                        ObjPosts.Status = EditPostObj.Status;
                        ObjPosts.StatusDate = EditPostObj.StatusDate;
                        ObjPosts.SlugInfoId = await _SlugServices.UpsertAndGetId(slugManagementVm);
                        ObjPosts.Title = EditPostObj.Title;
                        ObjPosts.CreatedById = CreatedById;
                        ObjPosts.CreationDate = DateTimeOffset.Now;
                        ObjPosts.PostContent = EditPostObj.PostContent;
                        ObjPosts.IsPrivate = EditPostObj.IsPrivate;
                        ObjPosts.Keywords = EditPostObj.Keywords;
                        ObjPosts.metaDescription = EditPostObj.metaDescription;
                        ObjPosts.addtionalMetaTags = EditPostObj.addtionalMetaTags;
                        ObjPosts.SiteId = EditPostObj.SiteId;
                        ObjPosts.Description = EditPostObj.Description;
                        ObjPosts.ShortDescription = EditPostObj.ShortDescription;
                        ObjPosts.ExpiryDate = EditPostObj.ExpiryDate;
                        ObjPosts.ThumbnailId = EditPostObj.ThumbnailId;
                        ObjPosts.RelatedCategoriesSettings = EditPostObj.RelatedCategoriesSettings;
                        ObjPosts.Settings = EditPostObj.Settings;
                        ObjPosts.CultureId = EditPostObj.CultureId;

                        //ObjPosts.ParentId = EditPostObj.ParentId;

                        _PostRepository.Add(ObjPosts);
                        await _PostRepository.SaveChangesAsync();
                   //     var AddDocumant = await _meiliRepo.AddDocument(EskaCMS.Infrastructure.Enums.EEntity.Post,ObjPost);

                        EditPostObj.Id = ObjPosts.Id;
                        ObjPosts.ParentId = EditPostObj.ParentId == 0 ? ObjPosts.Id : EditPostObj.ParentId;

                        await AddPostCategoriesTagsMediaAsync(EditPostObj, false);

                        await _PostRepository.SaveChangesAsync();
                        await DBTrans.CommitAsync();




                    }
                    else
                    {
                        if (EditPostObj.Id == 0)
                            throw new Exception("Post Id is missing");


                        cacheService.RemoveByPrefix( "Slug"+ ObjPosts.SlugInfo.Slug + ObjPosts.CultureId + EditPostObj.SiteId);
                        await clientCacheService.RemoveCacheFile(ObjPosts.SlugInfo.Slug, ObjPosts.SiteId, ObjPosts.CultureId);
                        Core.Models.SlugManagementVm slugManagementVm = new()
                        {
                            Id = ObjPost.SlugInfoId,
                            Slug = EditPostObj.Slug,
                            CultureId = EditPostObj.CultureId,
                            Entity = EEntity.Post,
                            SiteId = EditPostObj.SiteId

                        };
                        ObjPost.Status = EditPostObj.Status;
                        ObjPost.StatusDate = EditPostObj.StatusDate;
                        ObjPost.PostTemplateId = EditPostObj.TemplateId.Value;
                        slugManagementVm.Id = ObjPost.SlugInfoId;
                        ObjPost.SlugInfoId = await _SlugServices.UpsertAndGetId(slugManagementVm);
                        ObjPost.Title = EditPostObj.Title;
                        ObjPost.ModifiedById = EditPostObj.ModifiedById;
                        ObjPost.ModificationDate = EditPostObj.ModificationDate;
                        ObjPost.PostContent = EditPostObj.PostContent;
                        ObjPost.Description = EditPostObj.Description;
                        ObjPost.ShortDescription = EditPostObj.ShortDescription;
                        ObjPost.ExpiryDate = EditPostObj.ExpiryDate;
                        ObjPost.SiteId = EditPostObj.SiteId;
                        ObjPost.IsPrivate = EditPostObj.IsPrivate;
                        ObjPost.Keywords = EditPostObj.Keywords;
                        ObjPost.metaDescription = EditPostObj.metaDescription;
                        ObjPost.addtionalMetaTags = EditPostObj.addtionalMetaTags;
                        ObjPost.RelatedCategoriesSettings = EditPostObj.RelatedCategoriesSettings;
                        ObjPost.Settings = EditPostObj.Settings;
                        ObjPost.CultureId = EditPostObj.CultureId;
                        ObjPost.ThumbnailId = EditPostObj.ThumbnailId;
                        await _PostRepository.SaveChangesAsync();
                        ObjPosts.ParentId = EditPostObj.ParentId;
                        /*Delete old categories, tags ang media*/

                        //var c = await _PostsCategoriesRepository.Query().Where(x => x.PostId == EditPostObj.Id).ToListAsync();
                        //foreach (var item in c)
                        //{
                        //    _PostsCategoriesRepository.Remove(item);
                        //    await _PostsCategoriesRepository.SaveChangesAsync();
                        //}
                        //var t = await _PostTagsRepository.Query().Where(x => x.PostId == EditPostObj.Id).ToListAsync();
                        //foreach (var item in t)
                        //{
                        //    _PostTagsRepository.Remove(item);
                        //    await _PostTagsRepository.SaveChangesAsync();
                        //}
                        //var ExistingMedias = await _PostMediasRepository.Query().Where(x => x.PostId == EditPostObj.Id).ToListAsync();
                        //foreach (var item in ExistingMedias)
                        //{
                        //    _PostMediasRepository.Remove(item);
                        //    await _PostMediasRepository.SaveChangesAsync();
                        //}

                        EditPostObj.Id = ObjPost.Id;
                        await AddPostCategoriesTagsMediaAsync(EditPostObj);
                        await DBTrans.CommitAsync();
                    }
                    return ObjPosts.ParentId.Value;
                }
                catch (Exception exc)
                {
                    await DBTrans.RollbackAsync();
                    throw exc;
                }
            });
        }


        #endregion




        #region New Post Service. 
        public async Task<PostsListOutputVM<PostsViewModel>> GetPosts(PostsSearchVM search)
        {
            var SiteId = _workContext.GetCurrentSiteId();
            var CurrentUserId = await _workContext.GetCurrentUserId();

            var query = _PostRepository.Query()
                .Where(x => (x.SiteId == SiteId &&
                       (search.Statuses != null && search.Statuses.Any() ? search.Statuses.Contains(x.Status) : x.Status == EStatus.Published)) &&
                       (x.ExpiryDate == null || x.ExpiryDate.Value > DateTimeOffset.Now) &&
                       (x.PublishDate != null && x.PublishDate.Value < DateTimeOffset.Now)
                       && (search.CultureId == null || x.CultureId == search.CultureId)
                       && (!search.OnlyMyPosts || x.CreatedById == CurrentUserId))
                .Where(post => string.IsNullOrEmpty(search.PublishDate) || (search.PublishDate.Equals(post.PublishDate.Value.Year.ToString())) &&
                        (string.IsNullOrEmpty(search.PublishDateMonth) || (search.PublishDateMonth.Equals(post.PublishDate.Value.Month.ToString()))))
                .Where(post => string.IsNullOrEmpty(search.SearchValue)
                    || (post.Title.ToUpper().Contains(search.SearchValue.ToUpper())
                    || post.Id.ToString().ToUpper().Contains(search.SearchValue.ToUpper())
                    || post.SlugInfo.Slug.ToUpper().Contains(search.SearchValue.ToUpper())))
                .Where(post => (search.AutherId == 0 || search.AutherId == null || post.PostsAuthers.Any(a => a.userId == search.AutherId))
                    && (search.CategoriesIds.Count == 0 || post.PostsCategories.Any(c => search.CategoriesIds.Any(ci => c.CategoryId == ci))))
                .AsNoTracking();

            query = search.PostsSort != null
                ? SortPosts(search.PostsSort, query)
                : query.OrderByDescending(x => x.Id);

            int totalCount = await query.CountAsync();

            // Page in SQL, then load the graph. Includes are now actually used.
            var pagedEntities = await query
                .Skip(search.PageNo * search.PageSize ?? 0)
                .Take(search.PageSize ?? 0)
                .Include(p => p.SlugInfo)
                .Include(x => x.PostsAuthers).ThenInclude(x => x.user)
                .Include(x => x.Thumbnail)
                .Include(x => x.PostMedias).ThenInclude(x => x.Media)
                .Include(x => x.PostsCategories).ThenInclude(x => x.Category).ThenInclude(x => x.CategoryDetails)
                .AsSplitQuery()                       // avoids cartesian blow-up from 3 collection includes
                .ToListAsync();

            // Projection runs in memory now — the nested FirstOrDefault() ?? "" can't fail here.
            var outputList = pagedEntities.Select(x => new PostsViewModel
            {
                Id = x.Id,
                TemplateId = x.PostTemplateId,
                ThumbnailId = x.ThumbnailId,
                ThumbnailURL = x.Thumbnail?.PublicUrl,
                Slug = x.SlugInfo.Slug,
                Title = x.Title,
                CreatedById = x.CreatedById,
                CreationDate = x.CreationDate,
                ModifiedById = x.ModifiedById,
                ModificationDate = x.ModificationDate,
                Status = x.Status,
                StatusDate = x.StatusDate,
                PostContent = x.PostContent,
                PublishDate = x.PublishDate,
                IsPrivate = x.IsPrivate,
                ExpiryDate = x.ExpiryDate,
                SiteId = x.SiteId,
                Description = x.Description,
                ShortDescription = x.ShortDescription,
                Keywords = x.Keywords,
                Settings = x.Settings,
                CultureId = x.CultureId,
                ParentId = x.ParentId.Value,          // see caveat below
                RelatedCategoriesSettings = x.RelatedCategoriesSettings,
                MediaURLs = x.PostMedias.Select(y => new PostMediaWithURLViewModel
                {
                    Id = y.Id,
                    Url = y.Media.PublicUrl,
                }).ToList(),
                PostsCategories = x.PostsCategories.Select(y => new PostCategoriesVM
                {
                    CategoryId = y.CategoryId,
                    Id = y.Id,
                    CategoryName = y.Category.CategoryDetails
                        .Where(f => f.CultureId == x.CultureId)
                        .Select(c => c.Name)
                        .FirstOrDefault() ?? ""
                }).ToList(),
                PostAuthorIds = x.PostsAuthers.Select(a => new PostAuthorVM
                {
                    AuthorId = a.Id,
                    Name = a.user.FullName
                }).ToList(),
            }).ToList();

            return new PostsListOutputVM<PostsViewModel>
            {
                List = outputList,
                TotalCount = totalCount
            };
        }
        public async Task<bool> ChangeParentByCultureId(long siteId, long cultureId)
        {
            try
            {
                var Query = _PostRepository.Query().Where(x => x.SiteId == siteId && x.Status != EStatus.Deleted);
                var AllPosts = await Query.ToListAsync();

                var PostsEqualCulture = await Query.Where(x => x.CultureId == cultureId).ToListAsync();


                foreach (var post in PostsEqualCulture)
                {

                    var ReletivePosts = AllPosts.Where(x => x.ParentId == post.ParentId).ToList();


                    foreach (var CultureItem in ReletivePosts)
                    {
                        CultureItem.ParentId = post.Id;
                    }

                }

                await _PostRepository.SaveChangesAsync();


                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<PostsListOutputVM<PostsOutputVM>> GetPostsForAdmin(PostsSearchVM search)
        {
            try
            {
                var query = await _PostRepository.Query()
                    .Where(x => (
                      x.SiteId == search.SiteId && x.Status != EStatus.Deleted) &&
                      (search.CultureId == null || x.CultureId == search.CultureId) && (search.Statuses == null || search.Statuses.Contains(x.Status)))
                    .Include(p => p.SlugInfo)
                    .Include(x => x.PostsCategories)
                    .ThenInclude(x => x.Category)
                    .ThenInclude(x => x.CategoryDetails)
                    .Include(p => p.Culture)
                    .Include(p => p.Thumbnail)
                     .Include(x => x.LoginPage)
                      .Include("LoginPage.Children.SlugInfo")
                      .AsNoTrackingWithIdentityResolution()
                      .AsSplitQuery()
                   .ToListAsync();


                if (search.PostsSort != null)
                {
                    query = SortPostsForAdmin(search.PostsSort, query);
                }
                else
                {
                    query = query.OrderByDescending(x => x.Id).ToList();
                }

                return await BuildPostsTree(query, search);
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<PostsViewModel> GetPostById(long PostId)
        {
            try
            {
                var post= await _PostRepository.Query()
                            .Where(y => y.Id == PostId)
                            .Include(x => x.SlugInfo)
                            .Include(x => x.Thumbnail)

                            .Include(x => x.PostTemplate)
                            .ThenInclude(x => x.CurrentVersion.ImportedFiles)
                            .ThenInclude(x => x.Media)

                            .Include(x => x.PostsCategories)
                            .ThenInclude(x => x.Category)
                            .ThenInclude(x => x.CategoryDetails)
                             //  .ThenInclude(x => x.Tag)

                             .Include(x => x.LoginPage)

                            .Include(x => x.PostMedias)
                            .ThenInclude(x => x.Media)
                            .Select(x =>

                         new PostsViewModel
                         {
                             Id = x.Id,
                             TemplateId = x.PostTemplateId,
                             ThumbnailId = x.ThumbnailId,
                             ThumbnailURL = x.Thumbnail.PublicUrl,
                             IsLoginRequired = x.IsLoginRequired,

                             PostTemplate = new PostTemplate
                             {
                                 Id = x.PostTemplate.Id,
                                 Name = x.PostTemplate.Name,

                             },

                             Template = new Pages.Models.PageTemplateVM
                             {
                                 Description = x.PostTemplate.Description,
                                 AddtionalMetaTags = x.PostTemplate.AddtionalMetaTags,
                                 Name = x.PostTemplate.Name,
                                 CurrentVersion = new Pages.Models.TemplateVersionVM
                                 {
                                     CreatedById = x.PostTemplate.CurrentVersion.CreatedById,
                                     CreatedByName = x.PostTemplate.CurrentVersion.CreatedBy.FullName,
                                     CreationDate = x.PostTemplate.CurrentVersion.CreationDate,
                                     SettingsBody = x.PostTemplate.CurrentVersion.SettingsBody,
                                     VersionId = x.PostTemplate.CurrentVersion.VersionId,
                                     Css = x.PostTemplate.CurrentVersion.Css,
                                     Scripts = x.PostTemplate.CurrentVersion.Scripts,
                                     ImportFilesList = x.PostTemplate.CurrentVersion.ImportedFiles
                                                    .Select(i => new ImportedFilesVM()
                                                    {
                                                        Id = i.Id,
                                                        FileId = i.Media == null ? 0 : i.Media.Id,
                                                        FileName = i.Media == null ? string.Empty : i.Media.Filename,
                                                        FileUrl = i.Media == null ? string.Empty : i.Media.PublicUrl,
                                                        SourceId = x.PostTemplate.CurrentVersionId
                                                    }).ToList()
                                 },
                                 Type = (long)x.PostTemplate.Type
                             },

                             Slug = x.SlugInfo.Slug,
                             Title = x.Title,
                             CreatedById = x.CreatedById,
                             CreationDate = x.CreationDate,
                             ModifiedById = x.ModifiedById,
                             ModificationDate = x.ModificationDate,
                             Status = x.Status,
                             StatusDate = x.StatusDate,
                             PostContent = x.PostContent,
                             Description = x.Description,
                             ShortDescription = x.ShortDescription,
                             Keywords = x.Keywords,
                             metaDescription = x.metaDescription,
                             addtionalMetaTags = x.addtionalMetaTags,
                             SiteId = x.SiteId,
                             ParentId = x.ParentId.Value,
                             IsPrivate = x.IsPrivate,
                             CultureId = x.CultureId,
                             ExpiryDate = x.ExpiryDate,
                             CategoryList = x.PostsCategories.Select(y => new TempListsObj
                             {
                                 id = y.Id,
                                 label = y.Category.CategoryDetails == null ? "" : y.Category.CategoryDetails.Where(f => f.CultureId == x.CultureId).Select(x => x.Name).FirstOrDefault() ?? ""
                             }).ToList(),
                             //TagList = x.PostsTags.Select(y => new TempListsObj
                             //{
                             //    id = y.Id,
                             //    label = y.Tag.Title
                             //}).ToList(),
                             PostsCategories = x.PostsCategories.Select(y => new PostCategoriesVM

                             {
                                 CategoryId = y.CategoryId,
                                 CategoryName = y.Category.CategoryDetails == null ? "" : y.Category.CategoryDetails.Where(f => f.CultureId == x.CultureId).Select(x => x.Name).FirstOrDefault() ?? "",
                                 Id = y.Id,
                             }).ToList(),
                             //PostsTags = x.PostsTags.Select(y => new PostTagsVM
                             //{
                             //    Id = y.Id,
                             //    TagId = y.TagId,
                             //    TagName = y.Tag.Title

                             //}).ToList(),
                             RelatedCategoriesSettings = x.RelatedCategoriesSettings,
                             Settings = x.Settings,
                             MediaURLs = x.PostMedias.Select(y => new PostMediaWithURLViewModel
                             {
                                 Id = y.MediaId,
                                 Url = y.Media.PublicUrl,
                             }).ToList(),

                         }
                 ).FirstOrDefaultAsync();

                var postTags = await _TagsService.GetTagsByEntityType(new Core.Areas.Core.ViewModels.Tags.SearchEntitesViewModel
                {
                    EntityIds = new() { post.Id },
                    EntityType = EEntity.Post,
                    
                });

                post.PostsTags = postTags.ToList();
                return post;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<List<GetPostsCategories>> GetPostByCategoryId(long CategoryId, long CultureId)
        {
            try
            {
                return await _PostsCategoriesRepository.Query()
                    .Include(x => x.Category)
                    .Include(x => x.Post)
                    .ThenInclude(x => x.SlugInfo)
                   .Where(y => y.CategoryId == CategoryId && y.Category.CategoryDetails.Any(g => !g.CultureId.HasValue || g.CultureId == CultureId))



                   .Select(x => new GetPostsCategories
                   {
                       Id = x.Post.Id,
                       Slug = x.Post.SlugInfo.Slug,
                       Title = x.Post.Title,
                   }
                 ).ToListAsync();
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<List<PostsViewModel>> GetPostByParentId(long ParentId)
        {
            var entities = await _PostRepository.Query()
                .Where(y => y.ParentId == ParentId)
                .Include(x => x.SlugInfo)                                   // ADDED – used by projection
                .Include(x => x.Thumbnail)
                .Include(x => x.PostsAuthers).ThenInclude(x => x.user)      // ADDED .user
                .Include(x => x.Culture)
                .Include(x => x.PostTemplate).ThenInclude(x => x.CurrentVersion).ThenInclude(x => x.CreatedBy)        // ADDED CreatedBy
                .Include(x => x.PostTemplate).ThenInclude(x => x.CurrentVersion).ThenInclude(x => x.ImportedFiles).ThenInclude(i => i.Media)  // ADDED .Media
                .Include(x => x.PostsCategories).ThenInclude(x => x.Category).ThenInclude(x => x.CategoryDetails)
                .Include(x => x.PostMedias).ThenInclude(x => x.Media)
                .Include("LoginPage.Children.SlugInfo")
                .Include(p => p.PostRoles)
                .AsSplitQuery()
                .AsNoTracking()
                .ToListAsync();

            // Projection now runs in memory — the nested ImportedFiles / FirstOrDefault patterns can't fail here.
            var result = entities.Select(x => new PostsViewModel
            {
                Id = x.Id,
                TemplateId = x.PostTemplateId,
                ThumbnailId = x.ThumbnailId,
                ThumbnailURL = x.Thumbnail?.PublicUrl,
                IsLoginRequired = x.IsLoginRequired,
                LoginPageId = x.LoginPageId,
                PostTemplate = new PostTemplate
                {
                    Id = x.PostTemplate.Id,
                    Name = x.PostTemplate.Name,
                },
                CultureCode = x.Culture.CultureId,
                Template = new Pages.Models.PageTemplateVM
                {
                    Description = x.PostTemplate.Description,
                    AddtionalMetaTags = x.PostTemplate.AddtionalMetaTags,
                    Name = x.PostTemplate.Name,
                    CurrentVersion = new Pages.Models.TemplateVersionVM
                    {
                        CreatedById = x.PostTemplate.CurrentVersion.CreatedById,
                        CreatedByName = x.PostTemplate.CurrentVersion.CreatedBy.FullName,
                        CreationDate = x.PostTemplate.CurrentVersion.CreationDate,
                        SettingsBody = x.PostTemplate.CurrentVersion.SettingsBody,
                        VersionId = x.PostTemplate.CurrentVersion.VersionId,
                        Css = x.PostTemplate.CurrentVersion.Css,
                        Scripts = x.PostTemplate.CurrentVersion.Scripts,
                        ImportFilesList = x.PostTemplate.CurrentVersion.ImportedFiles
                            .Select(i => new ImportedFilesVM
                            {
                                Id = i.Id,
                                FileId = i.Media == null ? 0 : i.Media.Id,
                                FileName = i.Media == null ? string.Empty : i.Media.Filename,
                                FileUrl = i.Media == null ? string.Empty : i.Media.PublicUrl,
                                SourceId = x.PostTemplate.CurrentVersionId
                            }).ToList()
                    },
                    Type = (long)x.PostTemplate.Type
                },
                CultureId = x.CultureId,
                Slug = x.SlugInfo.Slug,
                LoginPage = x.LoginPage != null
                    ? x.LoginPage.Children.Where(c => c.CultureId == x.CultureId).Select(l => new LoginPageDetails
                    {
                        Id = l.Id,
                        Slug = l.SlugInfo.Slug,
                        Name = l.Name,
                        ParentId = l.ParentId.Value
                    }).FirstOrDefault()
                    : null,
                Title = x.Title,
                CreatedById = x.CreatedById,
                CreationDate = x.CreationDate,
                ModifiedById = x.ModifiedById,
                ModificationDate = x.ModificationDate,
                Status = x.Status,
                StatusDate = x.StatusDate,
                PostContent = x.PostContent,
                Description = x.Description,
                ShortDescription = x.ShortDescription,
                Keywords = x.Keywords,
                metaDescription = x.metaDescription,
                addtionalMetaTags = x.addtionalMetaTags,
                SiteId = x.SiteId,
                IsPrivate = x.IsPrivate,
                ExpiryDate = x.ExpiryDate,
                PublishDate = x.PublishDate,
                AuthorIds = x.PostsAuthers.Select(a => a.userId).ToList(),
                CategoryList = x.PostsCategories.Select(y => new TempListsObj
                {
                    id = y.Id,
                    label = y.Category.CategoryDetails == null ? ""
                        : y.Category.CategoryDetails.Where(f => f.CultureId == x.CultureId).Select(c => c.Name).FirstOrDefault() ?? "",
                        CategoryId = y.CategoryId.ToString()
				}).ToList(),
                PostsCategories = x.PostsCategories.Select(y => new PostCategoriesVM
                {
                    CategoryId = y.CategoryId,
                    CategoryName = y.Category.CategoryDetails == null ? ""
                        : y.Category.CategoryDetails.Where(f => f.CultureId == x.CultureId).Select(c => c.Name).FirstOrDefault() ?? "",
                    Id = y.Id,
                }).ToList(),
                RelatedCategoriesSettings = x.RelatedCategoriesSettings,
                Settings = x.Settings,
                MediaURLs = x.PostMedias.Select(y => new PostMediaWithURLViewModel
                {
                    Id = y.MediaId,
                    Url = y.Media.PublicUrl,
                    ContentType = y.Media.ContentType
                }).ToList(),
                PostsAuthor = x.PostsAuthers.Select(a => new PostAuthorVM
                {
                    PostId = a.postId,
                    AuthorId = a.Id,
                    Name = a.user.FullName,
                    UserId = a.user.Id,
                }).ToList(),
                PostsRolesIds = x.PostRoles.Select(r => r.RoleId).ToList(),
            }).ToList();

            // unchanged: tags post-processing
            var allIds = result.Select(x => x.Id).ToList();
            var user = await _workContext.GetCurrentUser();
            var postTags = await _TagsService.GetTagsByEntityType(new Core.Areas.Core.ViewModels.Tags.SearchEntitesViewModel
            {
                EntityIds = allIds,
                EntityType = EEntity.Post
            });

            foreach (var post in result)
            {
                var tags = postTags.Where(x => x.EntityId == post.Id).ToList();
                if (tags.Any())
                    post.PostsTags = tags;
            }

            return result;
        }

        public async Task<PostsViewModel> GetPostByCultures(long parentId, long cultureId)
        {
            try
            {
                return await _PostRepository.Query()
                            .Where(y => y.ParentId == parentId && y.CultureId == cultureId)

                            .Include(x => x.Thumbnail)

                            .Include(x => x.PostTemplate)
                            .ThenInclude(x => x.CurrentVersion.ImportedFiles)
                            .ThenInclude(x => x.Media)

                            .Include(x => x.PostsCategories)
                            .ThenInclude(x => x.Category)
                            .ThenInclude(x => x.CategoryDetails)
                            .Include(x => x.PostMedias)
                            .ThenInclude(x => x.Media)
                            .Select(x =>

                         new PostsViewModel
                         {
                             Id = x.Id,
                             TemplateId = x.PostTemplateId,
                             ThumbnailId = x.ThumbnailId,
                             ThumbnailURL = x.Thumbnail.PublicUrl,
                             PostTemplate = new PostTemplate
                             {
                                 Id = x.PostTemplate.Id,
                                 Name = x.PostTemplate.Name,

                             },

                             Template = new Pages.Models.PageTemplateVM
                             {
                                 Description = x.PostTemplate.Description,
                                 AddtionalMetaTags = x.PostTemplate.AddtionalMetaTags,
                                 Name = x.PostTemplate.Name,
                                 CurrentVersion = new Pages.Models.TemplateVersionVM
                                 {
                                     CreatedById = x.PostTemplate.CurrentVersion.CreatedById,
                                     CreatedByName = x.PostTemplate.CurrentVersion.CreatedBy.FullName,
                                     CreationDate = x.PostTemplate.CurrentVersion.CreationDate,
                                     SettingsBody = x.PostTemplate.CurrentVersion.SettingsBody,
                                     VersionId = x.PostTemplate.CurrentVersion.VersionId,
                                     Css = x.PostTemplate.CurrentVersion.Css,
                                     Scripts = x.PostTemplate.CurrentVersion.Scripts,
                                     ImportFilesList = x.PostTemplate.CurrentVersion.ImportedFiles
                                                    .Select(i => new ImportedFilesVM()
                                                    {
                                                        Id = i.Id,
                                                        FileId = i.Media == null ? 0 : i.Media.Id,
                                                        FileName = i.Media == null ? string.Empty : i.Media.Filename,
                                                        FileUrl = i.Media == null ? string.Empty : i.Media.PublicUrl,
                                                        SourceId = x.PostTemplate.CurrentVersionId
                                                    }).ToList()
                                 },
                                 Type = (long)x.PostTemplate.Type
                             },

                             Slug = x.SlugInfo.Slug,
                             Title = x.Title,
                             CreatedById = x.CreatedById,
                             CreationDate = x.CreationDate,
                             ModifiedById = x.ModifiedById,
                             ModificationDate = x.ModificationDate,
                             Status = x.Status,
                             StatusDate = x.StatusDate,
                             PostContent = x.PostContent,
                             Description = x.Description,
                             ShortDescription = x.ShortDescription,
                             Keywords = x.Keywords,
                             metaDescription = x.metaDescription,
                             addtionalMetaTags = x.addtionalMetaTags,
                             SiteId = x.SiteId,
                             ParentId = x.ParentId.Value,
                             IsPrivate = x.IsPrivate,
                             CultureId = x.CultureId,
                             ExpiryDate = x.ExpiryDate,
                             CategoryList = x.PostsCategories.Select(y => new TempListsObj
                             {
                                 id = y.Id,
                                 label = y.Category.CategoryDetails == null ? "" : y.Category.CategoryDetails.Where(f => f.CultureId == x.CultureId).Select(x => x.Name).FirstOrDefault() ?? ""
                             }).ToList(),
                             //TagList = x.PostsTags.Select(y => new TempListsObj
                             //{
                             //    id = y.Id,
                             //    label = y.Tag.Title
                             //}).ToList(),
                             PostsCategories = x.PostsCategories.Select(y => new PostCategoriesVM

                             {
                                 CategoryId = y.CategoryId,
                                 CategoryName = y.Category.CategoryDetails == null ? "" : y.Category.CategoryDetails.Where(f => f.CultureId == x.CultureId).Select(x => x.Name).FirstOrDefault() ?? "",
                                 Id = y.Id,
                             }).ToList(),
                             //PostsTags = x.PostsTags.Select(y => new PostTagsVM
                             //{
                             //    Id = y.Id,
                             //    TagId = y.TagId,
                             //    TagName = y.Tag.Title

                             //}).ToList(),
                             RelatedCategoriesSettings = x.RelatedCategoriesSettings,
                             Settings = x.Settings,
                             MediaURLs = x.PostMedias.Select(y => new PostMediaWithURLViewModel
                             {
                                 Id = y.MediaId,
                                 Url = y.Media.PublicUrl,
                             }).ToList(),

                         }
                 ).FirstOrDefaultAsync();
            }
            catch (Exception exc)
            {

                throw exc;
            }


        }
        public async Task<PostsViewModel> GetPostBySlug(string Slug)
        {
            try
            {
                if (string.IsNullOrEmpty(Slug))
                    throw new Exception("Slug Id is missing");
                //long PostId = _PostRepository.Query().Where(x => x.Slug.ToUpper() == Slug.ToUpper()).Select(x => x.Id).FirstOrDefault();

                //if (PostId == 0)
                //    throw new Exception("No Post Found");




                return await _PostRepository.Query()
                             .Include(x => x.SlugInfo)
                            .Where(x => x.SlugInfo.Slug == Slug)
                            .Include(x => x.Thumbnail)
                            .Include(x => x.PostTemplate)
                            .Include(x => x.PostsCategories)
                            .ThenInclude(x => x.Category)
                            .ThenInclude(x => x.CategoryDetails)
                            .Include(x => x.PostMedias)
                            .ThenInclude(x => x.Media)
                            .Select(x =>

                         new PostsViewModel
                         {
                             Id = x.Id,
                             PostsCategories = x.PostsCategories.Select(y => new PostCategoriesVM
                             {
                                 CategoryId = y.CategoryId,
                                 CategoryName = y.Category.CategoryDetails == null ? "" : y.Category.CategoryDetails.Where(f => f.CultureId == x.CultureId).Select(x => x.Name).FirstOrDefault() ?? "",
                                 Id = y.Id,
                             }).ToList(),
                             //PostsTags = x.PostsTags.Select(y => new PostTagsVM
                             //{
                             //    Id = y.Id,
                             //    TagId = y.TagId,
                             //    TagName = y.Tag.Title

                             //}).ToList(),
                             TemplateId = x.PostTemplateId,
                             Slug = x.SlugInfo.Slug,
                             Title = x.Title,
                             CreatedById = x.CreatedById,
                             CreationDate = x.CreationDate,
                             ModifiedById = x.ModifiedById,
                             ModificationDate = x.ModificationDate,
                             Status = x.Status,
                             StatusDate = x.StatusDate,
                             PostContent = x.PostContent,
                             Description = x.Description,
                             ShortDescription = x.ShortDescription,
                             Keywords = x.Keywords,
                             SiteId = x.SiteId,
                             IsPrivate = x.IsPrivate,
                             ExpiryDate = x.ExpiryDate,
                             MediaURLs = x.PostMedias.Select(y => new PostMediaWithURLViewModel
                             {
                                 Id = y.Id,
                                 Url = y.Media.PublicUrl,
                             }).ToList(),

                         }
                 ).FirstOrDefaultAsync();
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
        public async Task<List<PostGroupByYearVM>> GetPostByYearAndMonth(FilterForMonthsViewModel viewModel)
        {
            try
            {

                var culture = _workContext.GetCurrentCultureId();


                var result = await _PostRepository.Query()
                            .Where(x => (x.PublishDate != null && x.PublishDate.Value < DateTimeOffset.Now) && x.Status == EStatus.Published)
                            .Where(x => string.IsNullOrEmpty(viewModel.startYear) || Convert.ToInt32(viewModel.startYear) <= x.PublishDate.Value.Year)
                            .Where(x => string.IsNullOrEmpty(viewModel.Month) || Convert.ToInt32(viewModel.Month) == x.PublishDate.Value.Month)
                            .Include(x => x.SlugInfo)
                            .Include(x => x.Thumbnail)
                            .Include(x => x.PostTemplate)
                            .Include(x => x.PostsCategories)
                            .ThenInclude(x => x.Category)
                            .ThenInclude(x => x.CategoryDetails)
                            .Where(x => (viewModel.CategoriesIds == null || viewModel.CategoriesIds.Count == 0 || x.PostsCategories.Any(c => viewModel.CategoriesIds.Any(ci => c.CategoryId == ci))))
                            .Include(x => x.PostMedias)
                            .ThenInclude(x => x.Media)
                            .Include(x => x.Culture)
                            .Where(x => x.CultureId == culture)
                            .Select(x =>

                         new PostsViewModel
                         {
                             Id = x.Id,
                             PostsCategories = x.PostsCategories.Select(y => new PostCategoriesVM
                             {
                                 CategoryId = y.CategoryId,
                                 CategoryName = y.Category.CategoryDetails
                                               .Where(f => f.CultureId == culture)
                                               .Select(f => f.Name)
                                               .FirstOrDefault(),
                                 Id = y.Id,
                             }).ToList(),
                             //PostsTags = x.PostsTags.Select(y => new PostTagsVM
                             //{
                             //    Id = y.Id,
                             //    TagId = y.TagId,
                             //    TagName = y.Tag.Title

                             //}).ToList(),
                             TemplateId = x.PostTemplateId,
                             Slug = x.SlugInfo.Slug,
                             Title = x.Title,
                             CreatedById = x.CreatedById,
                             CreationDate = x.CreationDate,
                             ModifiedById = x.ModifiedById,
                             ModificationDate = x.ModificationDate,
                             Status = x.Status,
                             StatusDate = x.StatusDate,
                             PostContent = x.PostContent,
                             Description = x.Description,
                             ShortDescription = x.ShortDescription,
                             Keywords = x.Keywords,
                             SiteId = x.SiteId,
                             IsPrivate = x.IsPrivate,
                             CultureCode = x.Culture.CultureId,
                             ExpiryDate = x.ExpiryDate,
                             PublishDate = x.PublishDate.Value,
                             MediaURLs = x.PostMedias.Select(y => new PostMediaWithURLViewModel
                             {
                                 Id = y.Id,
                                 Url = y.Media.PublicUrl,
                             }).ToList(),

                         }
                     ).ToListAsync();




                var model = result.GroupBy(x => x.PublishDate.Value.Year).Select(x => new PostGroupByYearVM
                {
                    Year = x.Key,
                    TotalCount = x.Count(),

                    Month = x.GroupBy(p => p.PublishDate.Value.Month).Select(p => new PostGroupByMonthVM()
                    {
                        MonthName = p.FirstOrDefault().PublishDate.Value.ToString("MMMM", new CultureInfo(p.FirstOrDefault().CultureCode)),
                        MonthNumber = p.Key,
                        TotalCount = p.Count(),
                        post = p.Select(y => new PostModel
                        {
                            Title = y.Title,
                            PostId = y.Id
                        }).ToList()

                    })

                   .OrderBy(x => x.MonthNumber).ToList()

                }).OrderByDescending(x => x.Year)
                .ToList();


                return model;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<long> DuplicatePost(long ParentPostId, long postId, long UserId, long SiteId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
            await using var DBTrans = await _context.Database.BeginTransactionAsync();
            try
            {

                    var Posts = await _PostRepository.Query()
                                                     .Where(x => x.ParentId == ParentPostId)
                                                     .Include(x => x.SlugInfo)
                                                      .Include(x => x.PostsCategories)
                                                      .ThenInclude(x => x.Category)
                                                     .ThenInclude(x => x.CategoryDetails)
                                                     .Select(p => new PostsViewModel
                                                     {
                                                         UserId = UserId,
                                                         SiteId = SiteId,
                                                         addtionalMetaTags = p.addtionalMetaTags,
                                                         RelatedCategoriesSettings = p.RelatedCategoriesSettings,
                                                         CreatedById = UserId,
                                                         CreationDate = p.CreationDate,
                                                         CultureId = p.CultureId,
                                                         Description = p.Description,
                                                         ExpiryDate = p.ExpiryDate,
                                                         IsPrivate = p.IsPrivate,
                                                         Keywords = p.Keywords,
                                                         metaDescription = p.metaDescription,
                                                         ParentId = 0,
                                                         Slug = p.SlugInfo.Slug + " Duplicated" + Guid.NewGuid().ToString().Substring(1, 2),

                                                         Title = p.Title,
                                                         StatusDate = p.StatusDate,
                                                         PostContent = p.PostContent,
                                                         ShortDescription = p.ShortDescription,
                                                         ThumbnailId = p.ThumbnailId,
                                                         TemplateId = p.PostTemplateId,
                                                         PostsCategories = p.PostsCategories.Select(x => new PostCategoriesVM
                                                         {
                                                             PostId = x.PostId,
                                                             CategoryId = x.CategoryId,
                                                             CategoryName = x.Category.CategoryDetails == null ? "" : x.Category.CategoryDetails.Where(f => f.CultureId == p.CultureId).Select(x => x.Name).FirstOrDefault() ?? "",
                                                             CreatedById = x.CreatedById,
                                                             CreationDate = x.CreationDate,
                                                             ModifiedById = x.ModifiedById,
                                                             ModificationDate = x.ModificationDate,
                                                             ThumbnailImageId = x.Category.ThumbnailImageId,
                                                         }).ToList(),
                                                     }
                                                     ).ToListAsync();

                    long NewParentId = 0;
                    foreach (var _Post in Posts)
                    {
                        _Post.ParentId = NewParentId;
                        NewParentId = await AddPost(_Post, true);

                    }
                    await DBTrans.CommitAsync();
                    return NewParentId;
                }
                catch (Exception exc)
                {
                    await DBTrans.RollbackAsync();
                    return 0;
                }
            });

        }
        public async Task<List<Posts>> UpsertPostFirstStep(List<PostsViewModel> PostList, long UserId, long SiteId, bool isDuplicate = false)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                PostList = PostList.OrderByDescending(p => p.Status).ThenByDescending(x => x.Id).ToList();

                if (PostList[0].Id == 0)
                {
                    PostList = PostList.OrderByDescending(p => p.Slug).ToList();
                }
                List<Posts> RelativePosts = new List<Posts>();
            await using var DBTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                    long ParentId = 0;
                    foreach (var Post in PostList)
                    {
                        bool IsEditMode = Post.Id != 0;

                        //if (Post.Status == EStatus.Unpublished && Post.Id == 0)
                        //{
                        //    continue;
                        //}



                        var ObjPost = new Posts();
                        if (IsEditMode)
                        {
                            ObjPost = _PostRepository.Query().Include(x => x.SlugInfo).Where(p => p.Id == Post.Id).FirstOrDefault();

                            if (ObjPost == null)
                            {
                                throw new Exception("Post Not Found");
                            }

                        }


                        string Slug = Post.Slug;
                        if (!string.IsNullOrEmpty(Post.Settings))
                        {
                            PostSettingsVM SettingsVM = JsonConvert.DeserializeObject<PostSettingsVM>(Post.Settings);
                            if (SettingsVM.isExternal)
                            {
                                Slug = SettingsVM.link;
                            }
                        }
                        Core.Models.SlugManagementVm slugManagementVm = new()
                        {
                            Id = ObjPost.SlugInfoId,

                            Slug = Slug,

                            CultureId = Post.CultureId,
                            SiteId = SiteId,

                            Entity = EEntity.Post,

                        };
                        ObjPost.Status = Post.Status;
                        ObjPost.StatusDate = DateTime.Now;

                        if (ParentId == 0 && (string.IsNullOrEmpty(Post.Slug) || string.IsNullOrEmpty(Post.Title)))
                        {
                            throw new Exception("Slug And Title Are Required");
                        }

                        ObjPost.SlugInfoId = await _SlugServices.UpsertAndGetId(slugManagementVm);

                        ObjPost.Title = string.IsNullOrEmpty(Post.Title) ? RelativePosts[0].Title : Post.Title;

                        ObjPost.IsPrivate = Post.IsPrivate;
                        ObjPost.SiteId = SiteId;
                        ObjPost.ExpiryDate = Post.ExpiryDate;
                        ObjPost.IsLoginRequired = Post.IsLoginRequired;
                        ObjPost.LoginPageId = Post.LoginPageId;
                        ObjPost.ThumbnailId = Post.ThumbnailId;
                        ObjPost.Settings = Post.Settings;
                        ObjPost.PostTemplateId = Post.TemplateId.HasValue && Post.TemplateId.Value != 0 ? Post.TemplateId.Value : RelativePosts[0].PostTemplateId;
                        ObjPost.CultureId = Post.CultureId;
                        ObjPost.PublishDate = Post.PublishDate;

                        await ValidateStatusAndPublishDate_ExpireDate(ObjPost.Status, ObjPost.PublishDate, ObjPost.ExpiryDate);

                        if (!IsEditMode)
                        {
                            ObjPost.CreationDate = DateTimeOffset.Now;
                            ObjPost.CreatedById = UserId;

                            _PostRepository.Add(ObjPost);
                        }
                        else
                        {
                            cacheService.RemoveByPrefix("Slug" + ObjPost.SlugInfo.Slug + ObjPost.CultureId + SiteId );
                            await clientCacheService.RemoveCacheFile(ObjPost.SlugInfo.Slug, ObjPost.SiteId,Post.CultureId );
                            ObjPost.ModificationDate = DateTime.Now;
                            ObjPost.ModifiedById = UserId;
                        }

                        await _PostRepository.SaveChangesAsync();


                        Post.Id = ObjPost.Id;
                        ObjPost.ParentId = ParentId == 0 ? ObjPost.Id : ParentId;
                        ParentId = ParentId == 0 ? ObjPost.Id : ParentId;
                        if (Post.AuthorIds != null)
                        {
                            var oldPostAuthors = await _PostAuther.Query().Where(x => x.postId == Post.Id).ToListAsync();
                            _PostAuther.RemoveRange(oldPostAuthors);

                            foreach (var AuthId in Post.AuthorIds)
                            {
                                var PostAuth = new PostsAuther();
                                PostAuth.postId = Post.Id;
                                PostAuth.userId = AuthId;
                                _PostAuther.Add(PostAuth);

                            }
                            await _PostAuther.SaveChangesAsync();

                        }

                        await AddPostCategoriesTagsMediaAsync(Post, isDuplicate);

                        RelativePosts.Add(ObjPost);
                    }
                    await _PostRepository.SaveChangesAsync();
                    await DBTrans.CommitAsync();
                    return RelativePosts;

                }
                catch (Exception exc)
                {
                    await DBTrans.RollbackAsync();
                    if (exc.InnerException != null)
                    {
                        if (exc.InnerException.Message.Contains("Duplicate entry"))
                        {
                            throw new Exception("Slug Is Already Used");
                        }
                        else
                        {
                            throw new Exception(exc.InnerException.Message);
                        }
                    }
                    throw exc;
                }
            });
        }


        public async Task DeleteCategoryDetails(long Id)
        {

            try {
                var categoryDetails = await categoryDetailsRepository.Query().Where(x => x.Id == Id).FirstOrDefaultAsync();
				var postCategory = await _PostsCategoriesRepository.Query().Where(x => x.CategoryId == categoryDetails.CategoryId).FirstOrDefaultAsync();

				categoryDetailsRepository.Remove(categoryDetails);

				_PostsCategoriesRepository.Remove(postCategory);


				_PostsCategoriesRepository.SaveChangesAsync();
				categoryDetailsRepository.SaveChangesAsync();

			}
			catch (Exception exc)
			{
				throw exc;
			}


		}

		public async Task<long> UpsertPostForWeb(List<AddPostForWebVM> PostList, long UserId, long SiteId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                PostList = PostList.OrderBy(p => p.Id).ToList();

                if(PostList.Any(x=> x.Id == 0) && PostList.Any(x => x.Id != 0))
                {
                    PostList = PostList.OrderByDescending(x => x.Id).ToList();

                }

                if (PostList.All(x=>x.Id == 0))
                {
                    PostList = PostList.OrderBy(p => p.CultureId).ToList();
                }



                List<Posts> RelativePosts = new List<Posts>();
            await using var DBTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                    long ParentId = 0;
                    foreach (var Post in PostList)
                    {
                        bool IsEditMode = Post.Id != 0;





                        var ObjPost = new Posts();
                        if (IsEditMode)
                        {
                            ObjPost = _PostRepository.Query().Include(x => x.SlugInfo).Where(p => p.Id == Post.Id).FirstOrDefault();

                            if (ObjPost == null)
                            {
                                throw new Exception("Post Not Found");
                            }

                        }


                        string Slug = Post.Slug;
                        if (!string.IsNullOrEmpty(Post.Settings))
                        {
                            PostSettingsVM SettingsVM = JsonConvert.DeserializeObject<PostSettingsVM>(Post.Settings);
                            if (SettingsVM.isExternal)
                            {
                                Slug = SettingsVM.link;
                            }
                        }
                        Core.Models.SlugManagementVm slugManagementVm = new()
                        {
                            Id = ObjPost.SlugInfoId,

                            Slug = Slug,

                            CultureId = Post.CultureId,
                            SiteId = SiteId,
                            Entity = EEntity.Post,

                        };

                        ObjPost.Keywords = Post.Keywords;
                        ObjPost.metaDescription = Post.metaDescription;
                        ObjPost.addtionalMetaTags = Post.addtionalMetaTags;
                        ObjPost.RelatedCategoriesSettings = Post.RelatedCategoriesSettings;
                        ObjPost.Status = Post.Status;
                        ObjPost.StatusDate = DateTime.Now;

                        if (ParentId == 0 && (string.IsNullOrEmpty(Post.Slug) || string.IsNullOrEmpty(Post.Title)))
                        {
                            throw new Exception("Slug And Title Are Required");
                        }

                        ObjPost.SlugInfoId = await _SlugServices.UpsertAndGetId(slugManagementVm);

                        ObjPost.Title = string.IsNullOrEmpty(Post.Title) ? RelativePosts[0].Title : Post.Title;

                        ObjPost.IsPrivate = Post.IsPrivate;
                        ObjPost.SiteId = SiteId;
                        ObjPost.ExpiryDate = Post.ExpiryDate;




                        //var thumbnail = (await HandlePostAttachments(null, Post.Thumbnail, UserId, SiteId, Post.FolderId)).FirstOrDefault();

                        //ObjPost.ThumbnailId = Post.ThumbnailId.HasValue ? Post.ThumbnailId : thumbnail;
                        ObjPost.ThumbnailId =  Post.ThumbnailId;
                        ObjPost.Settings = Post.Settings;
                        ObjPost.PostTemplateId = Post.TemplateId.HasValue && Post.TemplateId.Value != 0 ? Post.TemplateId.Value : RelativePosts[0].PostTemplateId;
                        ObjPost.CultureId = Post.CultureId;
                        ObjPost.PublishDate = Post.PublishDate.HasValue ? Post.PublishDate.Value : DateTimeOffset.Now;

                        ObjPost.Description = Post.Description;
                        ObjPost.ShortDescription = Post.ShortDescription;
                        ObjPost.PostContent = Post.PostContent;




                        await ValidateStatusAndPublishDate_ExpireDate(ObjPost.Status, ObjPost.PublishDate, ObjPost.ExpiryDate);

                        if (!IsEditMode)
                        {
                            ObjPost.CreationDate = DateTimeOffset.Now;
                            ObjPost.CreatedById = UserId;

                            _PostRepository.Add(ObjPost);
                        }
                        else
                        {

                            ObjPost.ModificationDate = DateTime.Now;
                            ObjPost.ModifiedById = UserId;
                        }

                        await _PostRepository.SaveChangesAsync();




                        Post.Id = ObjPost.Id;
                        
                        ObjPost.ParentId = ParentId == 0 ? ObjPost.Id : ParentId;
 
                        ParentId = ParentId == 0 ? ObjPost.Id : ParentId;
                        if (Post.AuthorIds != null)
                        {
                            var oldPostAuthors = await _PostAuther.Query().Where(x => x.postId == Post.Id).ToListAsync();
                            _PostAuther.RemoveRange(oldPostAuthors);

                            foreach (var AuthId in Post.AuthorIds)
                            {
                                var PostAuth = new PostsAuther();
                                PostAuth.postId = Post.Id;
                                PostAuth.userId = AuthId;
                                _PostAuther.Add(PostAuth);

                            }
                            await _PostAuther.SaveChangesAsync();

                        }

                        await HandleAdditionalInfoForPost(Post, UserId, SiteId);

                        RelativePosts.Add(ObjPost);
                    }
                    await _PostRepository.SaveChangesAsync();
                    await DBTrans.CommitAsync();

                    return ParentId;

                }
                catch (Exception exc)
                {
                    await DBTrans.RollbackAsync();

                    if (exc.InnerException != null)
                    {
                        if (exc.InnerException.Message.Contains("Duplicate entry"))
                        {
                            throw new Exception("Slug Is Already Used");
                        }
                        else
                        {
                            throw new Exception(exc.InnerException.Message);
                        }
                    }
                    throw exc;
                }
            });

        }

        private async Task<List<long>> HandlePostAttachments(IFormFileCollection images,IFormFile thumbnail, long userId, long siteId, long? folderId)
        {

                var mediaUpload = new MediaUploadModel();

                if (thumbnail != null)
                {
                    mediaUpload.Uploads = new List<IFormFile> { thumbnail };

                }

                else if (images != null)
                {
                    mediaUpload.Uploads = images;
                }
                else
                {
                    return new List<long>();
                }


                mediaUpload.SiteId = siteId;
                mediaUpload.CreatedBy = userId;
                mediaUpload.Force = false;
                mediaUpload.ParentId = folderId.HasValue ? folderId : null;

                var media = await _mediaService.SaveMedia(mediaUpload);

             

                var mediaIds = media.Select(x => x.Id).ToList();

                return mediaIds;


            
        }


        public async Task<bool> CompleteUpsertPost(List<CompleteUpsertPostVM> Posts)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
            await using var DBTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                    foreach (var Post in Posts)
                    {
                        var ObjPost = new Posts();

                        ObjPost =await _PostRepository.Query().Where(p => p.Id == Post.Id).FirstOrDefaultAsync();

                        if (ObjPost == null)
                        {
                            throw new Exception("Post Not Found");
                        }

                        ObjPost.Keywords = Post.Keywords;
                        ObjPost.metaDescription = Post.metaDescription;
                        ObjPost.addtionalMetaTags = Post.addtionalMetaTags;
                        ObjPost.RelatedCategoriesSettings = Post.RelatedCategoriesSettings;
                        ObjPost.PostContent = Post.PostContent;
                        ObjPost.Description = Post.Description;
                        ObjPost.ShortDescription = Post.ShortDescription;
                        if (Post.Tags != null)
                        {
                           await HandlePostTags(Post.Tags, ObjPost.Id);
                        }
                    }
                    await _PostRepository.SaveChangesAsync();
                    await DBTrans.CommitAsync();
                    return true;


                }
                catch (Exception Exc)
                {
                    await DBTrans.RollbackAsync();

                    throw Exc;
                }
            });
        }



        public async Task<bool> UpdatePostStatus(long PostId, long? ModifiedById, EStatus Status)
        {
            try
            {
                var ObjPosts = await _PostRepository.Query().Include(x => x.SlugInfo).Where(x => x.ParentId == PostId || x.Id == PostId).AsTracking().ToListAsync();
                foreach (var ObjPost in ObjPosts)
                {
                    await ValidateStatusAndPublishDate_ExpireDate(Status, ObjPost.PublishDate, ObjPost.ExpiryDate);
                    ObjPost.Status = Status;
                    ObjPost.StatusDate = DateTimeOffset.Now;
                    ObjPost.ModifiedById = ModifiedById;
                    ObjPost.ModificationDate = DateTimeOffset.Now;

                    if (Status == EStatus.Deleted)
                    {


                        Core.Models.SlugManagementVm slugManagementVm = new()
                        {
                            Id = ObjPost.SlugInfoId,
                            CultureId = ObjPost.CultureId,

                            Slug = PostId + "Deleted" + await _SlugServices.GetSlugById(ObjPost.SlugInfoId),
                            Entity = EEntity.Post,


                        };
                        ObjPost.SlugInfoId = await _SlugServices.UpsertAndGetId(slugManagementVm);

                    }
                    await _PostRepository.SaveChangesAsync();
                    var IsMeliEnabled = _config.GetValue<bool>("MeiliSearch:Enabled");
                    if (IsMeliEnabled&&(Status == EStatus.Deleted || Status == EStatus.Unpublished))
                    {
                        //var elasticId = $"{PostId}-{nameof(EEntity.Post)}";
                        //await _elasticRepo.DeleteDocuemnt(elasticId);
                        ///MeiliSearch
                        var MeiliId = $"{PostId}-{nameof(EEntity.Post)}";
                        await _meiliRepo.DeleteDocuemnt(MeiliId, nameof(EEntity.Post));
                    }
                }
                return true;
            }
            catch (Exception exc)
            {
                throw exc;
            }

        }
        public async Task<bool> UpdateRelativePostsStatus(long ParentPostId, long? ModifiedById, EStatus Status)
        {
            try
            {
                var Posts = await _PostRepository.Query().Include(x => x.SlugInfo).Where(x => x.ParentId == ParentPostId).ToListAsync();

                foreach (var ObjPosts in Posts)
                {

                    ObjPosts.Status = Status;
                    ObjPosts.StatusDate = DateTimeOffset.Now;
                    ObjPosts.ModifiedById = ModifiedById;
                    ObjPosts.ModificationDate = DateTimeOffset.Now;
                    if (Status == EStatus.Deleted)
                    {
                        Core.Models.SlugManagementVm slugManagementVm = new()
                        {
                            Id = ObjPosts.SlugInfoId,
                            CultureId = ObjPosts.CultureId,
                            Slug = ObjPosts.Id + "Deleted" + await _SlugServices.GetSlugById(ObjPosts.SlugInfoId),
                            Entity = EEntity.Post,
                        };
                        ObjPosts.SlugInfoId = await _SlugServices.UpsertAndGetId(slugManagementVm);
                    }
                }
                await _PostRepository.SaveChangesAsync();

                foreach (var ObjPosts in Posts)
                {

                    if (Status == EStatus.Deleted || Status == EStatus.Unpublished)
                    {
                        var elasticId = $"{ObjPosts.Id}-{nameof(EEntity.Post)}";
                        await _elasticRepo.DeleteDocuemnt(elasticId);
                        var IsMeliEnabled = _config.GetValue<bool>("MeiliSearch:Enabled");
                        if (IsMeliEnabled)
                        {
                            ///MeiliSearch
                            var MeiliId = $"{ObjPosts}-{nameof(EEntity.Post)}";
                            await _meiliRepo.DeleteDocuemnt(MeiliId);
                        }
                    }
                }

                return true;
            }
            catch (Exception exc)
            {
                throw exc;
            }

        }


        public async Task<bool> UpdatePostsTemplate(string PostsIds, int TemplateId, long UserId, long SiteId)
        {
            try
            {
                int[] ids = PostsIds.Split(',').Select(n => Convert.ToInt32(n)).ToArray();
                var postList = await _PostRepository.Query().Where(x => x.Status != EStatus.Deleted
                        && x.SiteId == SiteId).Select(x => x).ToListAsync();

                for (int i = 0; i < postList.Count(); i++)
                {
                    for (int j = 0; j < ids.Count(); j++)
                    {
                        if (ids[j] == postList[i].Id)
                        {
                            postList[i].PostTemplateId = TemplateId;
                            postList[i].ModifiedById = UserId;
                            await _PostRepository.SaveChangesAsync();
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> UpdateStatusPosts(string PostsIds, long UserId, EStatus NewStatus)
        {
            try
            {
                List<long> postIds = PostsIds.Split(',').Select(n => Convert.ToInt64(n)).ToList();


                var PostList = await _PostRepository
                                    .Query()
                                    .Where(x => postIds.Any(item => item == x.Id))
                                    .ToListAsync();

                foreach (var PostItem in PostList)
                {
                    await ValidateStatusAndPublishDate_ExpireDate(NewStatus, PostItem.PublishDate, PostItem.ExpiryDate);
                    PostItem.Status = NewStatus;
                    PostItem.ModifiedById = UserId;
                    var IsMeliEnabled = _config.GetValue<bool>("MeiliSearch:Enabled");
                    if (IsMeliEnabled)
                        if (PostItem.Id == PostItem.ParentId)
                    {
                        
                        var MeiliId = $"{PostItem.Id}-{nameof(EEntity.Post)}";
                        await _meiliRepo.DeleteDocuemnt(MeiliId, nameof(EEntity.Post));
                    }
                    await _PostRepository.SaveChangesAsync();

                }

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task<RolesViewModel> GetPostAssignedRoles(long PostId, long SiteId)
        {
            try
            {
                var data = new RolesViewModel();

                data.assingedRoles = await _postRoleRepository.Query().Include(x => x.Role).Where(x => x.PostId == PostId).Select(x =>
                 new Role
                 {
                     Id = x.Role.Id,
                     Name = x.Role.Name,
                     Type = x.Role.Type,
                 }).ToListAsync();


                var unassingedRoles = await _roleManager.Roles.Where(x => x.SiteId == SiteId || x.SiteId == null).Select(x =>
                   new Role
                   {
                       Id = x.Id,
                       Name = x.Name,
                       Type = x.Type,
                   }).ToListAsync();


                var found = false;
                data.unassingedRoles = new List<Role>();
                foreach (var item in unassingedRoles)
                {
                    found = false;
                    foreach (var item2 in data.assingedRoles)
                    {
                        if (item.Id == item2.Id)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        data.unassingedRoles.Add(item);
                    }

                }


                return data;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
        public async Task<bool> SavePostAssginedRoles(RolesViewModel vm)
        {
            try
            {
                var oldRoles = await _postRoleRepository.Query().Where(x => x.PostId == vm.Id).Select(x => x).ToListAsync();
                foreach (var item in oldRoles)
                {
                    _postRoleRepository.Remove(item);
                }

                var userId = await _workContext.GetCurrentUserId();

                await _postRoleRepository.SaveChangesAsync();
                var RelatedPost = await _PostRepository.Query().Where(x => x.ParentId == vm.Id).ToListAsync();
                foreach (var value in RelatedPost)
                {
                    foreach (var item in vm.assingedRoles)
                    {
                        PostRoles newRole = new PostRoles();
                        newRole.PostId = value.Id;
                        newRole.RoleId = item.Id;
                        newRole.CreatedById = userId;
                        _postRoleRepository.Add(newRole);
                        await _postRoleRepository.SaveChangesAsync();

                    }
                    await _postRoleRepository.SaveChangesAsync();
                }
                return true;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<bool> SetLoginPage(SetPostLoginPageVM model)
        {
            try
            {
                long SiteId = await _workContext.GetCurrentSiteIdAsync();
                long UserId = await _workContext.GetCurrentUserId();


                var posts = _PostRepository.Query().Where(x => model.ParentsPostId.Contains(x.ParentId.Value) && x.SiteId == SiteId).ToList();


                foreach (var post in posts)
                {
                    post.LoginPageId = model.LoginPageId;
                    post.ModificationDate = DateTimeOffset.Now;
                    post.ModifiedById = UserId;
                    post.IsLoginRequired = model.LoginPageId.HasValue;

                }



                await _PostRepository.SaveChangesAsync();

                return true;


            }
            catch (Exception e)
            {

                throw e;
            }

        }

    //    public async Task<List<PostTagsVM>> GetAllPostsTags(long SiteId)
    //    {
    //        try
    //        {


    ////            var query = _TagsRepository.Query()
    ////.Where(x => x.SiteId == SiteId &&
    ////           (!EntityType.HasValue || x.EntityTags.Any(et => et.EntityType == EntityType.Value)));





    //            return await _TagsRepository.Query().Include(x => x.EntityTags)
    //              .Where(x => x.SiteId == SiteId &&
    //                        ( x.EntityTags.Any(et => et.EntityType == EEntity.Product)))
    //              .Select(y => new PostTagsVM
    //                {
    //                    Id = y.Id,
    //                    TagName = y.Title
    //                }).ToListAsync();
    //        }
    //        catch (Exception exc)
    //        {

    //            throw exc;
    //        }
    //    }

        public async Task<long> AddPost(PostsViewModel Post, bool isDuplicate = false)
        {

            try
            {
                var ObjPost = new Posts();
                ObjPost.PostTemplateId = Post.TemplateId.Value;
                ObjPost.Status = Post.Status;
                ObjPost.StatusDate = Post.StatusDate;
                Core.Models.SlugManagementVm slugManagementVm = new()
                {
                    CultureId = Post.CultureId,

                    Slug = Post.Slug,
                    Entity = EEntity.Post,
                    SiteId = Post.SiteId


                };
                var SlugId = await _SlugServices.UpsertAndGetId(slugManagementVm);
                ObjPost.SlugInfoId = SlugId;
                ObjPost.Title = Post.Title;
                ObjPost.CreatedById = Post.CreatedById;
                ObjPost.CreationDate = DateTimeOffset.Now;
                ObjPost.PostContent = Post.PostContent;
                ObjPost.IsPrivate = Post.IsPrivate;
                ObjPost.Keywords = Post.Keywords;
                ObjPost.metaDescription = Post.metaDescription;
                ObjPost.addtionalMetaTags = Post.addtionalMetaTags;
                ObjPost.SiteId = Post.SiteId;
                ObjPost.Description = Post.Description;
                ObjPost.ShortDescription = Post.ShortDescription;
                ObjPost.ExpiryDate = Post.ExpiryDate;
                ObjPost.ThumbnailId = Post.ThumbnailId;
                ObjPost.RelatedCategoriesSettings = Post.RelatedCategoriesSettings;
                ObjPost.Settings = Post.Settings;
                ObjPost.CultureId = Post.CultureId;
                _PostRepository.Add(ObjPost);
                await _PostRepository.SaveChangesAsync();
                Post.Id = ObjPost.Id;
                ObjPost.ParentId = Post.ParentId == 0 ? ObjPost.Id : Post.ParentId;


                await AddPostCategoriesTagsMediaAsync(Post, isDuplicate);

                await _PostRepository.SaveChangesAsync();

                return ObjPost.ParentId.Value;
            }
            catch (Exception exc)
            {
                throw exc;
            }

        }

        public async Task<List<PostAuthorVM>> GetPostAuthorsByPostId(long siteId, long PostId)
        {
            try
            {
                var postAuthors = await _PostAuther.Query().Where(l => l.postId == PostId).Include(x => x.user)
                    .Select(a => new PostAuthorVM
                    {
                        AuthorId = a.Id,
                        UserId = a.userId,
                        Name = a.user.FullName,
                        PostId = a.postId
                    }).ToListAsync();

                return postAuthors;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        public async Task<PostCreatedByInfo> GetCreatedByInfo(long PostId)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();

                var targetPost =await  _PostRepository.Query()
                                                .Where(x => x.Id == PostId && x.SiteId == siteId)
                                                .Include(x => x.CreatedBy)
                                                .AsNoTracking()
                                                .Select(x => new PostCreatedByInfo
                                                {
                                                    UserId = x.CreatedBy.Id,
                                                    EncryptedUserId = EncryptDecryptHelper.Encrypt(x.CreatedBy.Id.ToString()),
                                                    Email = x.CreatedBy.Email,
                                                    FullName = x.CreatedBy.FullName,
                                                    PhoneNumber = x.CreatedBy.PhoneNumber,
                                                    PostId = PostId,
                                                    UserName = x.CreatedBy.UserName,

                                                }).FirstOrDefaultAsync();


                if(targetPost is null)
                {
                    throw new Exception("Post doesn't exist");
                }

                return targetPost;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        #region HelperFunctions
        private async Task<bool> ValidateStatusAndPublishDate_ExpireDate(EStatus status, DateTimeOffset? publishDate, DateTimeOffset? unPublishDate)
        {

            //if (status == EStatus.Published && (unPublishDate.HasValue && unPublishDate.Value < DateTimeOffset.Now))
            //{
            //    throw new Exception("Please deactive unpublish date or Add a valid date for unpublish date  to schedule unpublish process");
            //}

            //if (status == EStatus.Unpublished && (publishDate.HasValue && publishDate.Value < DateTimeOffset.Now))
            //{
            //    throw new Exception("Please deactive publish date or Add a valid date for publish date  to schedule publish process");
            //}

            if ((publishDate.HasValue && unPublishDate.HasValue) && (unPublishDate.Value < publishDate.Value || unPublishDate.Value == publishDate.Value))
            {
                throw new Exception("Please Add a valid range for publish and unpublish date");
            }

            return true;
        }

        private async Task<PostsListOutputVM<PostsOutputVM>> BuildPostsTree(List<Posts> AllPosts, PostsSearchVM search)
        {
            List<PostsOutputVM> PostTree = new List<PostsOutputVM>();

            PostTree = AllPosts.Where(p => p.ParentId == p.Id).Select(p => new PostsOutputVM
            {
                Id = p.Id,
                Title = p.Title,
                Categories = p.PostsCategories.Select(x => x.Category).SelectMany(x => x.CategoryDetails).Where(x => x.CultureId == p.CultureId).Select(x => x.Name).ToList() ?? new List<string>(),
                ExpirationDate = p.ExpiryDate,
                CreationDate = p.CreationDate,
                PublishDate = p.PublishDate,
                ParentId = p.ParentId.Value,
                CultureId = p.CultureId,
                IsLoginRequired = p.IsLoginRequired,
                Description = p.Description,
                Status = AllPosts.Any(x => x.Status == EStatus.Published && x.ParentId == p.Id) ? EStatus.Published : EStatus.Unpublished,
                Slug = p.SlugInfo.Slug,
                ThumbnailUrl = p.Thumbnail == null ? "" : p.Thumbnail.PublicUrl,

                PostCultures = AllPosts.Where(c => c.ParentId == p.Id).Select(c => new PostCulture()
                {
                    Slug = c.SlugInfo.Slug,
                    Id = c.Id,
                    ParentId = c.ParentId.Value,
                    IsLoginRequired = c.IsLoginRequired,
                    LoginPageDetails = c.LoginPage != null ? c.LoginPage.Children.Where(x => x.CultureId == c.CultureId).Select(l => new LoginPageDetails()
                    {
                        Id = l.Id,
                        Slug = l.SlugInfo.Slug,
                        Name = l.Name,
                        ParentId = l.ParentId.Value
                    }).FirstOrDefault() : null,
                    Status = c.Status,
                    Culture = c.Culture,
                    PostsCategories = c.PostsCategories.Select(c => new PostsCategoriesOutVM()
                    {
                        PostId = c.PostId,
                        CategoryId = c.CategoryId,
                        Category = c.Category,
                        ModificationDate = c.ModificationDate,
                        CreatedById = c.CreatedById,
                        CreationDate = c.CreationDate,
                    }).ToList(),
                    Title = c.Title


                }).ToList()
            }).ToList();

            var FilteredQuery = PostTree
                            .Where(p => p.PostCultures
                                                .Any(post =>
                                                            (string.IsNullOrEmpty(search.SearchValue) || (post.Title.ToUpper().Contains(search.SearchValue) || post.Id.ToString().Contains(search.SearchValue) || post.Slug.ToUpper().Contains(search.SearchValue)))
                                                            && (string.IsNullOrEmpty(search.CultureCode) || post.Culture.CultureId == search.CultureCode)
                                                            && (search.CategoriesIds.Count == 0 || post.PostsCategories.Any(c => search.CategoriesIds.Any(ci => c.CategoryId == ci)))
                                                            ));

            var result = FilteredQuery
                        .Skip(search.PageNo * search.PageSize ?? 0)
                        .Take(search.PageSize ?? 0)
                        .ToList();



            return new PostsListOutputVM<PostsOutputVM>
            {
                List = result,
                TotalCount = FilteredQuery.Count()
            };
        }



        //private async Task<bool> UpsertPostTags(CompleteUpsertPostVM PostObj)
        //{
        //    try
        //    {
        //        var UserId = await _workContext.GetCurrentUserId();
        //        var SiteId = await _workContext.GetCurrentSiteIdAsync();

        //        var existingPostTags =await _PostTagsRepository.Query().Where(x => x.PostId == PostObj.Id).ToListAsync();
        //        if (existingPostTags.Count > 0)
        //        {
        //           await RemovePostTags(existingPostTags);
        //        }

        //        foreach (var TagItem in PostObj.Tags)
        //        {
        //            if (TagItem.Id != 0 && TagItem.PostId != 0)
        //            {
        //                continue;
        //            }

        //            long TagId = await GetOrCreateTagId(TagItem.TagName, SiteId, UserId);

        //            var newPostTag = new PostsTags
        //            {
        //                PostId = PostObj.Id,
        //                TagId = TagId,
        //                CreatedById = UserId,
        //                CreationDate = DateTimeOffset.Now
        //            };

        //            _PostTagsRepository.Add(newPostTag);
        //            await _PostTagsRepository.SaveChangesAsync();
        //        }

        //        return true;
        //    }
        //    catch (Exception Exc)
        //    {
        //        throw Exc;
        //    }
        //}

        //private async Task RemovePostTags(IEnumerable<PostsTags> postTags)
        //{
        //    try
        //    {
        //    _PostTagsRepository.RemoveRange(postTags);
        //    _PostTagsRepository.SaveChangesAsync();

        //    }
        //    catch (Exception ex)
        //    {

        //        throw;
        //    }
        //}


        private async Task HandleAdditionalInfoForPost(AddPostForWebVM postObj , long user, long SiteId )
        {
            try
            {
              
                var deletedItems = _PostsCategoriesRepository.Query().Where(x => x.PostId == postObj.Id).ToList();

                if (deletedItems.Count > 0)
                    _PostsCategoriesRepository.RemoveRange(deletedItems);
                if (postObj.PostsCategories != null && postObj.PostsCategories.Any())
                {
                    foreach (var PostCat in postObj.PostsCategories)
                    {
                        long CategoryId = 0;
                        if (PostCat.CategoryId == 0)
                        {
                            var NewCategory = new CategoryViewModel();
                            NewCategory.Name = PostCat.CategoryName;
                            NewCategory.UserId = user;
                            NewCategory.CreatedById = user;
                            NewCategory.Slug = PostCat.CategoryName;
                            NewCategory.SiteId = SiteId;
                            NewCategory.CategoryTypeId = ECategoryTypes.Post;
                            NewCategory.ThumnailImageId = postObj.ThumbnailId;
                            NewCategory.CultureId = postObj.CultureId;
                            CategoryId = await AddCategory(NewCategory);
                        }
                        if (CategoryId == 0)
                            CategoryId = PostCat.CategoryId;

                        var PC = new PostsCategories();
                        PC.PostId = postObj.Id;
                        PC.CategoryId = CategoryId;
                        PC.CreatedById = user;
                        PC.CreationDate = DateTimeOffset.Now;
                        _PostsCategoriesRepository.Add(PC);

                    }

                }


                await _PostsCategoriesRepository.SaveChangesAsync();



                var postMediaList = await _PostMediasRepository.Query().Where(x => x.PostId == postObj.Id).ToListAsync();


                //var newAttachmentIds = await HandlePostAttachments(postObj.Images, null, user, SiteId, postObj.FolderId);


                //var allIds = postObj.MediaURLs != null && postObj.MediaURLs.Any() ? newAttachmentIds.Concat(postObj.MediaURLs.Select(x => x.Id.Value).ToList()) : newAttachmentIds;

                //var mediaToBeRemoved = postMediaList.Where(x => !allIds.Any(y => y == x.MediaId)).ToList();

                var mediaToBeRemoved = postMediaList.Where(x => !postObj.MediaURLs.Any(y => y.Id == x.MediaId))
                                                               .ToList();

                var newAttachmentIds = postObj.MediaURLs.Where(x => !postMediaList.Any(y => y.MediaId == x.Id))
                                                         .ToList();



                if (mediaToBeRemoved.Any())
                {

                    _PostMediasRepository.RemoveRange(mediaToBeRemoved);
                }

                

                if (newAttachmentIds.Any())
                {
                    foreach (var Media in newAttachmentIds)
                    {
                        var ObjPostMedia = new PostMedias();
                        ObjPostMedia.PostId = postObj.Id;
                        ObjPostMedia.MediaId = Media.Id.Value;
                        ObjPostMedia.CreatedById = user;
                        ObjPostMedia.CreationDate = DateTimeOffset.Now;
                        _PostMediasRepository.Add(ObjPostMedia);
                    }
                }

                await _PostMediasRepository.SaveChangesAsync();



                if (postObj.Tags != null && postObj.Tags.Any())
                {
                    await _TagsService.UpdateTag(new TagsEntityViewModel { tags = postObj.Tags, EntityId = postObj.Id, EntityType = EEntity.Post });
                }


                if (postObj.PostsRolesIds != null && postObj.PostsRolesIds.Any())
                {

                    var roleToBeRemoved = await _postRoleRepository.Query().Where(x => x.PostId == postObj.Id)
                                                                           .ToListAsync();

                    if (roleToBeRemoved.Any())
                    {
                        _postRoleRepository.RemoveRange(roleToBeRemoved);
                    }
                    var rolesToBeAdded = new List<PostRoles>();

                    foreach (var roleId in postObj.PostsRolesIds)
                    {
                        PostRoles newRole = new PostRoles();
                        newRole.PostId = postObj.Id;
                        newRole.RoleId = roleId;
                        newRole.CreatedById = user;
                        rolesToBeAdded.Add(newRole);
                       

                    }
                    _postRoleRepository.AddRange(rolesToBeAdded);
                    await _postRoleRepository.SaveChangesAsync();
                }


            }
            catch (Exception exc)
            {
                throw exc;
            }
        }
        async Task AddPostCategoriesTagsMediaAsync(PostsViewModel Postobj, bool isDublicate = false)
        {
            try
            {
                long user = await _workContext.GetCurrentUserId();
                long SiteId = await _workContext.GetCurrentSiteIdAsync();

                if (!isDublicate)
                {

                    var deletedItems = _PostsCategoriesRepository.Query().Where(x => x.PostId == Postobj.Id).ToList();

                    if (deletedItems.Count > 0)
                        _PostsCategoriesRepository.RemoveRange(deletedItems);
                    if (Postobj.PostsCategories != null)
                    {
                        foreach (var PostCat in Postobj.PostsCategories)
                        {
                            long CategoryId = 0;
                            if (PostCat.CategoryId == 0)
                            {
                                var NewCategory = new CategoryViewModel();
                                NewCategory.Name = PostCat.CategoryName;
                                NewCategory.UserId = user;
                                NewCategory.CreatedById = user;
                                NewCategory.Slug = PostCat.CategoryName;
                                NewCategory.SiteId = SiteId;
                                NewCategory.CategoryTypeId = ECategoryTypes.Post;
                                NewCategory.ThumnailImageId = Postobj.ThumbnailId;
                                NewCategory.CultureId = Postobj.CultureId;
                                CategoryId = await AddCategory(NewCategory);
                            }
                            if (CategoryId == 0)
                                CategoryId = PostCat.CategoryId;

                            var PC = new PostsCategories();
                            PC.PostId = Postobj.Id;
                            PC.CategoryId = CategoryId;
                            PC.CreatedById = user;
                            PC.CreationDate = DateTimeOffset.Now;
                            _PostsCategoriesRepository.Add(PC);

                        }

                    }


                    await _PostsCategoriesRepository.SaveChangesAsync();
                    var postMediaList = await _PostMediasRepository.Query().Where(x => x.PostId == Postobj.Id).ToListAsync();
                    if (postMediaList.Count() > 0)

                        _PostMediasRepository.RemoveRange(postMediaList);


                    //This Code No Longer Needed: yazan
                    //if (Postobj.Media != null)
                    //{
                    //    foreach (var Media in Postobj.Media)
                    //    {
                    //        var ObjPostMedia = new PostMedias();
                    //        ObjPostMedia.PostId = Postobj.Id;
                    //        ObjPostMedia.MediaId = Media;
                    //        ObjPostMedia.CreatedById = user;
                    //        ObjPostMedia.CreationDate = DateTimeOffset.Now;
                    //        _PostMediasRepository.Add(ObjPostMedia);
                    //    }
                    //}

                    if (Postobj.MediaURLs != null)
                    {
                        foreach (var Media in Postobj.MediaURLs)
                        {
                            var ObjPostMedia = new PostMedias();
                            ObjPostMedia.PostId = Postobj.Id;
                            ObjPostMedia.MediaId = Media.Id.Value;
                            ObjPostMedia.CreatedById = user;
                            ObjPostMedia.CreationDate = DateTimeOffset.Now;
                            _PostMediasRepository.Add(ObjPostMedia);
                        }
                    }

                    await _PostMediasRepository.SaveChangesAsync();


                }
                else
                {
                    //categories
                    if (Postobj.PostsCategories.Count > 0)
                    {
                        foreach (var PostCat in Postobj.PostsCategories)
                        {
                            if (PostCat.CategoryId != 0)
                            {
                                var PC = new PostsCategories();
                                PC.PostId = Postobj.Id;
                                PC.CategoryId = PostCat.CategoryId;
                                PC.CreatedById = user;
                                PC.CreationDate = DateTimeOffset.Now;
                                _PostsCategoriesRepository.Add(PC);
                                await _PostsCategoriesRepository.SaveChangesAsync();
                            }
                        }

                    }

                }

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }




        private async Task<long> AddCategory(CategoryViewModel newCat)
        {
            try
            {
                long UserId = await _workContext.GetCurrentUserId();
                long SiteId = _workContext.GetCurrentSiteId();

                var Category = new Category();

                Category.SiteId = SiteId;
                Category.CategoryTypeId = Convert.ToInt64(newCat.CategoryTypeId);
                Category.CreatedById = UserId;
                Category.IncludeInMenu = newCat.IncludeInMenu;
                Category.StatusDate = DateTime.Now;
                Category.CreationDate = DateTime.Now;
                Category.ThumbnailImageId = newCat.ThumnailImageId;
                Category.ParentId = newCat.ParentId == 0 ? null : newCat.ParentId;
                Category.Status = EStatus.Published;
                categoryRepository.Add(Category);
                await categoryRepository.SaveChangesAsync();
                var SlugInfo = new SlugManagementVm
                {
                    CultureId = newCat.CultureId,
                    // Was EEntity.Page, which is what caused the duplicate CategoryDetails.SlugInfoId.
                    // AddIfNotExsist only refuses to reuse an existing slug row when the entity type
                    // DIFFERS; declaring a category as a Page made it match real Page slugs, so it
                    // silently returned that Page's SlugInfoId. The new CategoryDetails then collided
                    // with an existing row on the unique index IX(CultureId, SlugInfoId) of
                    // Core_CategoryDetails. With the correct type the clash is reported as
                    // "Slug Is Already Exists In Page" instead of a duplicate-key error.
                    // EEntity.Category also matches every other category slug write in the solution
                    // (CategoryService.cs: 265, 548, 565, 720, 774).
                    Entity = EEntity.Category,
                    SiteId = SiteId,
                    Slug = newCat.Slug

                };
                var CategoryDetails = new CategoryDetails();
                CategoryDetails.CategoryId = Category.Id;
                CategoryDetails.CategoryTemplateId = newCat.CategoryTemplateId;
                CategoryDetails.CultureId = newCat.CultureId;
                CategoryDetails.Name = newCat.Name;
                CategoryDetails.SlugInfoId = await _SlugServices.UpsertAndGetId(SlugInfo);


				// NOTE: do not re-enable the guard below. Skipping the Add when the slug is taken
				// creates a Category row with NO CategoryDetails, silently losing the name,
				// template and slug - it hides the duplicate-key error by corrupting data instead.
				// The real cause was the slug entity type above; the conflict is now reported
				// by AddIfNotExsist with a readable message.
				//var categoryDetailsBySlugId = await categoryDetailsRepository.Query()
				//	 .Where(x => x.SlugInfoId == CategoryDetails.SlugInfoId)
				//	 .FirstOrDefaultAsync();
                     //if(categoryDetailsBySlugId ==null){
					categoryDetailsRepository.Add(CategoryDetails);
				//}
				

                await categoryDetailsRepository.SaveChangesAsync();
                return Category.Id;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private IQueryable<Posts> SortPosts(PostsSearchSortVM search, IQueryable<Posts> query)
        {
            if (search.SortType == null)
            {
                search.SortType = PostsSearchSortTypeEnum.desc;

            }


            if (search.SortField == PostsSearchSortFieldsEnum.Id)
            {
                if (search.SortType == PostsSearchSortTypeEnum.asc)
                {
                    query = query.OrderBy(x => x.Id);
                }
                else
                {
                    query = query.OrderByDescending(x => x.Id);
                }
            }
            if (search.SortField == PostsSearchSortFieldsEnum.Title)
            {
                if (search.SortType == PostsSearchSortTypeEnum.asc)
                {
                    query = query.OrderBy(x => x.Title);
                }
                else
                {
                    query = query.OrderByDescending(x => x.Title);
                }
            }
            if (search.SortField == PostsSearchSortFieldsEnum.PublishDate)
            {
                if (search.SortType == PostsSearchSortTypeEnum.asc)
                {
                    query = query.OrderBy(x => x.PublishDate);
                }
                else
                {
                    query = query.OrderByDescending(x => x.PublishDate);
                }
            }
            if (search.SortField == PostsSearchSortFieldsEnum.ExpiryDate)
            {
                if (search.SortType == PostsSearchSortTypeEnum.asc)
                {
                    query = query.OrderBy(x => x.ExpiryDate);
                }
                else
                {
                    query = query.OrderByDescending(x => x.ExpiryDate);
                }
            }
            if (search.SortField == PostsSearchSortFieldsEnum.Description)
            {
                if (search.SortType == PostsSearchSortTypeEnum.asc)
                {
                    query = query.OrderBy(x => x.Description);
                }
                else
                {
                    query = query.OrderByDescending(x => x.Description);
                }
            }
            return query;

        }
        private List<Posts> SortPostsForAdmin(PostsSearchSortVM search, List<Posts> query)
        {
            if (search.SortField == PostsSearchSortFieldsEnum.Id)
            {
                if (search.SortType == PostsSearchSortTypeEnum.asc)
                {
                    query = query.OrderBy(x => x.Id).ToList();
                }
                else
                {
                    query = query.OrderByDescending(x => x.Id).ToList();
                }
            }
            if (search.SortField == PostsSearchSortFieldsEnum.Title)
            {
                if (search.SortType == PostsSearchSortTypeEnum.asc)
                {
                    query = query.OrderBy(x => x.Title).ToList();
                }
                else
                {
                    query = query.OrderByDescending(x => x.Title).ToList();
                }
            }
            if (search.SortField == PostsSearchSortFieldsEnum.PublishDate)
            {
                if (search.SortType == PostsSearchSortTypeEnum.asc)
                {
                    query = query.OrderBy(x => x.PublishDate).ToList();
                }
                else
                {
                    query = query.OrderByDescending(x => x.PublishDate).ToList();
                }
            }
            if (search.SortField == PostsSearchSortFieldsEnum.ExpiryDate)
            {
                if (search.SortType == PostsSearchSortTypeEnum.asc)
                {
                    query = query.OrderBy(x => x.ExpiryDate).ToList();
                }
                else
                {
                    query = query.OrderByDescending(x => x.ExpiryDate).ToList();
                }
            }
            if (search.SortField == PostsSearchSortFieldsEnum.Description)
            {
                if (search.SortType == PostsSearchSortTypeEnum.asc)
                {
                    query = query.OrderBy(x => x.Description).ToList();
                }
                else
                {
                    query = query.OrderByDescending(x => x.Description).ToList();
                }
            }
            return query;

        }

        private bool IsCategoryExist(int[] ids, List<CategoryViewModel> categoryList)
        {

            if (ids.Count() > 0)
            {
                foreach (var i2 in ids)
                {
                    foreach (var i3 in categoryList)
                    {
                        if (i2 == i3.Id)
                            return true;
                    }
                }
                return false;
            }
            else
                return true;
        }

        public async Task<List<PostsViewModel>> GetPostsByTagId(long siteId, long TageId)
        {
            try
            {
                //var Posts = await _PostTagsRepository.Query().Where(x => x.TagId == TageId)
                //            .Include(x => x.Post)
                //            .Include(x => x.Tag)
                //            .Include("Post.SlugInfo")
                //            .Include("Post.Thumbnail")
                //            .Include("Post.PostTemplate")
                //            .Include("Post.PostsCategories")
                //            .Include("Post.PostsCategories.Category")
                //            .Include("Post.PostsCategories.Category.CategoryDetails")
                //            .Include("Post.PostMedias")
                //            .Include("Post.PostMedias.Media")
                //            .Select(x => x.Post)
                //            .Select(x =>

                //         new PostsViewModel
                //         {
                //             Id = x.Id,
                //             PostsCategories = x.PostsCategories.Select(y => new PostCategoriesVM
                //             {
                //                 CategoryId = y.CategoryId,
                //                 CategoryName = y.Category.CategoryDetails == null ? "" : y.Category.CategoryDetails.Where(f => f.CultureId == x.CultureId).Select(x => x.Name).FirstOrDefault() ?? "",
                //                 Id = y.Id,
                //             }).ToList(),
                //             PostsTags = x.PostsTags.Select(y => new PostTagsVM
                //             {
                //                 Id = y.Id,
                //                 TagId = y.TagId,
                //                 TagName = y.Tag.Title

                //             }).ToList(),
                //             TemplateId = x.PostTemplateId,
                //             Slug = x.SlugInfo.Slug,
                //             Title = x.Title,
                //             CreatedById = x.CreatedById,
                //             CreationDate = x.CreationDate,
                //             ModifiedById = x.ModifiedById,
                //             ModificationDate = x.ModificationDate,
                //             Status = x.Status,
                //             StatusDate = x.StatusDate,
                //             PostContent = x.PostContent,
                //             Description = x.Description,
                //             ShortDescription = x.ShortDescription,
                //             Keywords = x.Keywords,
                //             SiteId = x.SiteId,
                //             IsPrivate = x.IsPrivate,
                //             ExpiryDate = x.ExpiryDate,
                //             MediaURLs = x.PostMedias.Select(y => new PostMediaWithURLViewModel
                //             {
                //                 Id = y.Id,
                //                 Url = y.Media.PublicUrl,
                //             }).ToList(),

                //         }
                // ).ToListAsync();
                return new List<PostsViewModel>();

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<bool> HandlePostTags(List<PostTagsVM> Tags , long postId)
        {
            try
            {
                var existingTags = await _EntityTagsRepository.Query()
                            .AsNoTracking()
                            .Where(x => x.EntityId == postId && x.EntityType == EEntity.Post)
                            .ToListAsync();

                if (existingTags.Count > 0)
                {
                    _EntityTagsRepository.RemoveRange(existingTags);
                    _EntityTagsRepository.SaveChanges();
                }
                var TagsEntityViewModel = new TagsEntityViewModel();
                TagsEntityViewModel.EntityType = EEntity.Post;
                TagsEntityViewModel.EntityId = postId;
                TagsEntityViewModel.tags = Tags.Select(x => new TagsViewModel { Title = x.TagName }).ToList();

                await _TagsService.CreateTag(TagsEntityViewModel);
                return true;
            }
            catch (Exception ex)
            {

                throw;
            }
        
        }

        #endregion

        #endregion

    }
}
