

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Models;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using AutoMapper;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Data;
using EskaCMS.Core.ViewModels;
using System.IO;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Core.Services;
using System.Security.Policy;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.DataAccess.Data;
using EskaCMS.Entities.Entity.MediaEntity;


namespace EskaCMS.Core.Repositories
{
    public class MediaRepository : IMediaRepository
    {

        private readonly IRepository<Media> _mediaRepository;
        private readonly IRepository<MediaDetails> _MediaDetailsRepository;
        private readonly IRepository<FolderRole> folderRoleRepository;
        private readonly IRepository<MediaFoldersDetails> _mediafoldersDetailsRepository;
        private readonly IRepository<MediaFolders> _mediafoldersRepository;
        private readonly IRepository<MediaVersion> _mediaversionsRepository;
        private readonly IWorkContext _WorkContext;
        private readonly IUserSiteService userSiteService;
        private readonly IMapper _mapper;
        private readonly EskaDCMSDbContext _context;
        class FolderCount
        {
            public long SiteId { get; set; }
            public long? FolderId { get; set; }
            public long? Order { get; set; }
            public int Count { get; set; }
        }



        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="db">The current db context</param>
        public MediaRepository(
            IRepository<Media> mediaRepository,
            IRepository<MediaFolders> mediafoldersRepository,
            IMapper mapper,
            IRepository<MediaVersion> mediaversionsRepository,
            IRepository<MediaDetails> MediaDetailsRepository,
            IRepository<FolderRole> FolderRoleRepository,
            IRepository<MediaFoldersDetails> mediafoldersDetailsRepository,
            IWorkContext WorkContext,
            IUserSiteService userSiteService,
            EskaDCMSDbContext context)
        {
            _mediaRepository = mediaRepository;
            _mediafoldersRepository = mediafoldersRepository;
            _mapper = mapper;
            _mediaversionsRepository = mediaversionsRepository;
            _WorkContext = WorkContext;
            this.userSiteService = userSiteService;
            _MediaDetailsRepository = MediaDetailsRepository;
            folderRoleRepository = FolderRoleRepository;
            _context = context;
            _mediafoldersDetailsRepository = mediafoldersDetailsRepository;

        }

        /// <summary>
        /// Gets all media available in the specified folder.
        /// </summary>
        /// <param name="folderId">The optional folder id</param>
        /// <returns>The available media</returns>
        public async Task<IEnumerable<long>> GetAll(long siteId, long? folderId = null, string SearchText = null , EStatus? Status = EStatus.Published, List<string> ContentType = null)
        {

            var UserId = await _WorkContext.GetCurrentUserId();
            var userRoles = await userSiteService.GetUserRoles(siteId, UserId);

            var Query = _mediaRepository.Query()
                  .Where(m => m.SiteId == siteId && m.Status == Status)
                  .Include(m => m.MediaDetails)
                  .Include(x=>x.MediaRoles)
                  .Where(x => x.MediaRoles.Any() ? x.MediaRoles.Any(y => userRoles.Contains(y.RoleId)) : true)
                  .AsNoTracking();
                  //.OrderBy(m => m.Filename);

           
           
                Query =  Query.Where(x => x.FolderId == folderId);

            
            if (!string.IsNullOrEmpty(SearchText))
            {
                var Lower = SearchText.ToLower();
                //var Lower = SearchText.ToLower();
                Query =  Query.Where(x => x.Filename.ToLower().Contains(Lower));
            }
           

             if(ContentType != null && ContentType.Any())
            {
                
                Query =  Query.Where(x => ContentType.Any(y=> y == x.ContentType));
            }

            if (Query.Any())
            {
                return await Query.Select(x=>x.Id).ToListAsync().ConfigureAwait(false);
            }

            return new List<long>();

        }

        public async Task<IEnumerable<long>> GetAllByMultibleFolders(long siteId, List<long> folderIds)
        {


            var Query = _mediaRepository.Query()
                  .Where(m => m.SiteId == siteId)
                  .AsNoTracking()
                  .Include(m => m.MediaDetails)
                  .OrderBy(m => m.Filename);
           
                return await Query.Where(x => folderIds.Contains(x.FolderId.Value)).Select(x => x.Id).ToListAsync().ConfigureAwait(false);

        }

        public async Task<List<long>> GetAllChildrenFolders(long siteId, long folderId,List<MediaFolders> allFolders)
        {


            
            //search in root folders;
            var targetFolder = allFolders.Where(x=>x.Id ==folderId).FirstOrDefault();

            if(targetFolder is null)
            {
                // search in children folders
                
                var foldersWithChildren = allFolders.SelectMany(f => GetFoldersRecursive(f))
                                              .ToList();

                targetFolder = foldersWithChildren.Where(x => x.Id == folderId).FirstOrDefault();

            }

            var folders = targetFolder.ChildFolders
                            .SelectMany(f => GetFoldersRecursive(f))
                            .ToList();

            var folderIds = folders.OrderByDescending(folder => GetFolderDepth(folder, folders)).Select(x=>x.Id).ToList();

            folderIds.Add(folderId);

            return folderIds;

        }

        public async Task<List<MediaFolders>> GetAllFolders()
        {
            var siteId = _WorkContext.GetCurrentSiteId();
            var allFolders = await _mediafoldersRepository.Query()
                                                    .Where(x=>x.SiteId == siteId)
                                                    .AsNoTracking()
                                                    .ToListAsync();
            var rootFolders = allFolders.Where(x => x.ParentId == null).ToList();

            foreach(var root in rootFolders)
            {
                root.ChildFolders = GetChildFolders(allFolders, root.Id);
            }

            return rootFolders;
        }

        public List<MediaFolders> GetChildFolders(List<MediaFolders> allFolders, long parentId)
        {

            return allFolders.Where(x => x.ParentId == parentId).Select(c => { c.ChildFolders = GetChildFolders(allFolders, c.Id); return c; }).ToList();
           
        }

        List<long> GetFolderIdsRecursive(MediaFolders folder)
        {
            var ids = new List<long> { folder.Id };

            if (folder.ChildFolders != null && folder.ChildFolders.Any())
            {
                foreach (var childFolder in folder.ChildFolders)
                {
                    ids.AddRange(GetFolderIdsRecursive(childFolder));
                }
            }

            return ids;
        }
        
        List<MediaFolders> GetFoldersRecursive(MediaFolders folder)
        {
            var folders = new List<MediaFolders> { folder };

            if (folder.ChildFolders != null && folder.ChildFolders.Any())
            {
                foreach (var childFolder in folder.ChildFolders)
                {
                    folders.AddRange(GetFoldersRecursive(childFolder));
                }
            }

            return folders;
        }

        public int GetFolderDepth(MediaFolders folder, List<MediaFolders> allFolders)
        {
           

            int depth = 0;
            long parentId = folder.ParentId.Value; 

            while (parentId != 0 && allFolders.Any(x=>x.ParentId == parentId))
            {
                depth++;
                parentId = allFolders.Where(x=>x.Id == parentId).FirstOrDefault()?.ParentId ?? 0;
            }

            return depth;
        }

        /// <summary>
        /// <inheritdoc cref="IMediaRepository.CountAll"/>
        /// </summary>
        /// <param name="folderId"></param>
        /// <returns></returns>
        public Task<int> CountAll(long? folderId) =>
            _mediaRepository.Query()
                .AsNoTracking()
                .Where(m => m.FolderId == folderId).CountAsync();

        /// <summary>
        /// Gets all media folders available in the specified
        /// folder.
        /// </summary>
        /// <param name="folderId">The optional folder id</param>
        /// <returns>The available media folders</returns>
        public async Task<IEnumerable<long>> GetAllFolders(long siteId, long? folderId = null) =>
            await _mediafoldersRepository.Query()
                .AsNoTracking()
                .Where(f => f.ParentId == folderId && f.SiteId == siteId)
                //.OrderBy(f => f.Name)
                .Select(f => f.Id)
                .ToListAsync()
                .ConfigureAwait(false);

        /// <summary>
        /// Get media for all Ids in this enumerable.
        /// </summary>
        /// <param name="ids">One or several media id</param>
        /// <returns>The matching media</returns>
        public Task<IEnumerable<MediaVM>> GetById(params long[] ids)
        {

            var Medias = _mediaRepository.Query().AsNoTracking()
              .Include(c => c.Versions)
              .Where(m => ids.Contains(m.Id))
              .Include(m => m.MediaDetails)
              .OrderBy(m => m.Filename)
              .ToArrayAsync()
              .ContinueWith(t => t.Result.Select(m => (MediaVM)m));
            return Medias;
        }
        /// <summary>
        /// Gets the media with the given id.
        /// </summary>
        /// <param name="id">The unique id</param>
        /// <returns>The media</returns>
        public Task<MediaVM> GetById(long id) =>
          _mediaRepository.Query()
                .AsNoTracking()
                .Include(m => m.Versions)
                .Include(m => m.MediaDetails)
                .FirstOrDefaultAsync(m => m.Id == id).ContinueWith(t => (MediaVM)t.Result);

        public Task<List<Media>> GetByFileName(string filename, long siteId) =>
        _mediaRepository.Query()
        .AsNoTracking()
        .Where(m => m.SiteId == siteId && m.Filename.Equals(filename))
            .Include(m => m.MediaDetails).ToListAsync();

        public Task<List<MediaDetailsWithOrderVM>> GetMediaDetails(long MediaId, long CultureId = 0) =>
        _MediaDetailsRepository.Query()
        .AsNoTracking()
        .Where(m =>
                    m.MediaId == MediaId
                    && (CultureId == 0 || m.CultureId == CultureId)
            )
            .Include(m => m.Media)
            .Select(m => new MediaDetailsWithOrderVM()
            {
                CreatedById = m.CreatedById,
                CultureId = m.CultureId,
                Culture = m.Culture,
                Description = m.Description,
                AltText = m.AltText,
                Order = m.Media.Order,
                Id = m.Id,
                Media = m.Media,
                MediaId = m.MediaId,
                ModifiedById = m.ModifiedById,
                Title = m.Title
            }).ToListAsync();



        public Task<List<MediaFolderVM>> GetByFolderName(string folderCode, long siteId) =>
        _mediafoldersRepository.Query()
        .AsNoTracking()
        .Where(m => m.SiteId == siteId).Select(x => new MediaFolderVM
        {
            Id = x.Id,
            ParentId = x.ParentId,
            Order = x.Order
        }).ToListAsync();


        /// <summary>
        /// Gets the media folder with the given id.
        /// </summary>
        /// <param name="id">The unique id</param>
        /// <returns>The media folder</returns>
        public async Task<MediaFolderVM> GetFolderById(long siteId, long id, long CultureId)
        {
            try
            {
                var UserId = await _WorkContext.GetCurrentUserId();
                var userRoles = await userSiteService.GetUserRoles(siteId, UserId);

                var Result = await _mediafoldersRepository.Query()
                    .AsNoTracking()
                    .Include(m => m.MediaFoldersDetails)
                    .Where(f => f.Id == id && f.SiteId == siteId)
                    .Include(x => x.FolderRoles)
                    .Where(x => x.FolderRoles.Any() ? x.FolderRoles.Any(y => userRoles.Contains(y.RoleId)) : true)
                    .Select(f => new MediaFolderVM
                    {
                        Id = f.Id,
                        ParentId = f.ParentId,
                        Title = f.MediaFoldersDetails.FirstOrDefault(m => m.CultureId == CultureId || CultureId == 0) == null
                                    ? ""
                                    : f.MediaFoldersDetails.FirstOrDefault(m => m.CultureId == CultureId || CultureId == 0).Title,

                        Description = f.MediaFoldersDetails.FirstOrDefault(m => m.CultureId == CultureId || CultureId == 0) == null
                                    ? ""
                                    : f.MediaFoldersDetails.FirstOrDefault(m => m.CultureId == CultureId || CultureId == 0).Description,

                        ThumbnailId = f.MediaFoldersDetails.FirstOrDefault(m => m.CultureId == CultureId || CultureId == 0) == null
                                    ? 0
                                    : f.MediaFoldersDetails.FirstOrDefault(m => m.CultureId == CultureId || CultureId == 0).ThumbnailId,
                        MediaFoldersDetails = f.MediaFoldersDetails.ToList(),
                        Order = f.Order
                    })
                    .FirstOrDefaultAsync();

                return Result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
         
        }
        /// <summary>
        /// Gets the hierachical media structure.
        /// </summary>
        /// <returns>The media structure</returns>
        public async Task<MediaStructure> GetStructure(long siteId, string search, long CultureId = 0, EStatus? Status = EStatus.Published, EMediaFilter? orderFilter = EMediaFilter.NewToOld)
        {
            var userId = await _WorkContext.GetCurrentUserId();

            var userRoles = await userSiteService.GetUserRoles(siteId, userId);

            var folders = _mediafoldersRepository.Query()
                .Where(x =>
                        x.SiteId == siteId &&
                    (x.MediaFoldersDetails.FirstOrDefault(m => m.CultureId == CultureId) != null
                        || CultureId == 0) &&
                        (Status == EStatus.Published ? x.Status == Status : true)
                    )
                .Include(f => f.MediaFoldersDetails)
                .ThenInclude(f => f.Thumbnail)
                .Include(x => x.FolderRoles)
                .Include(x => x.ChildFolders)
                .ThenInclude(x => x.MediaFoldersDetails)
                .Where(x => x.Status != EStatus.Deleted)
                .Where(x => x.FolderRoles.Any() ? x.FolderRoles.Any(y => userRoles.Contains(y.RoleId)) : true)
                .OrderByDescending(x => x.Id)
                .AsNoTracking();
            //if (!string.IsNullOrEmpty(search))
            //{
            var result = await folders.ToListAsync().ConfigureAwait(false);

            if (!string.IsNullOrEmpty(search))
            {

                // var filtered = await folders
                //.Where(x => x.MediaFoldersDetails.Any(y => y.Title.ToLower().Contains(search.ToLower())
                //    || x.ChildFolders.Any(c => c.MediaFoldersDetails.Any(d => d.Title.ToLower().Contains(search.ToLower())))))
                // .ToListAsync();
                var filtered = await folders
                    .Where(x => x.MediaFoldersDetails.Any(y => y.Title.ToLower().Contains(search.ToLower()))
                               || x.ChildFolders.Any(x => x.MediaFoldersDetails.Select(x => x.Title.ToLower()).Contains(search.ToLower()))).ToListAsync();
                //walk up the tree to include parents
                var finalList = new List<MediaFolders>(filtered);
                foreach (var f in filtered)
                {
                    //if a parent matches check the children
                    if (f.MediaFoldersDetails.Any(d => d.Title.ToLower().Contains(search.ToLower())))
                    {
                        if (!filtered.Any(x => x.Id == f.Id))
                        {
                            finalList.Add(f);

                            f.ChildFolders = f.ChildFolders?
                                .Where(c => c.MediaFoldersDetails.Any(d => d.Title.ToLower().Contains(search.ToLower())))
                                .ToList();
                        }
                    }
                    else
                    {    //if a child matches but not parent so include the parent and children that only matches
                        var parent = folders.FirstOrDefault(p => p.Id == f.ParentId);
                        if (parent != null)
                        {
                            var parentCopy = new MediaFolders
                            {
                                Id = parent.Id,
                                ParentId = parent.ParentId,
                                Status = parent.Status,
                                MediaFoldersDetails = parent.MediaFoldersDetails,
                                FolderRoles = parent.FolderRoles,
                                ChildFolders = parent.ChildFolders
                                    ?.Where(c => c.MediaFoldersDetails.Any(d => d.Title.ToLower().Contains(search.ToLower())))
                                    .GroupBy(x => x.Id) //to remove duplicate
                                    .Select(g => g.First())
                                    .ToList()
                            };

                            if (!finalList.Any(x => x.Id == parentCopy.Id))
                                finalList.Add(parentCopy);
                        }
                    }

                }
                result = finalList.ToList();
            }
        

                if (orderFilter.HasValue)
            {
                switch (orderFilter)
                {
                    case EMediaFilter.AscendingByOrder:
                        folders = folders.OrderBy(x => x.Order);
                        break;
                    case EMediaFilter.DescendingByOrder:
                        folders = folders.OrderByDescending(x => x.Order);
                        break;
                    case EMediaFilter.OldToNew:
                        folders = folders.OrderBy(x => x.Id);
                        break;
                }
            }

            
            var count = await _mediaRepository.Query()
                .AsNoTracking()
                .Include(x=>x.MediaRoles)
                .Where(x => x.MediaRoles.Any() ? x.MediaRoles.Any(y => userRoles.Contains(y.RoleId)) : true)
                .Where(x => x.SiteId == siteId && x.Status == Status)
                .GroupBy(m => m.FolderId)
                .Select(m => new FolderCount
                {
                    FolderId = m.Key,
                    Count = m.Count()

                })
                .ToListAsync()
                .ConfigureAwait(false);

           

            

            return Sort(result, count, CultureId, Status.Value);
        }
       

        /// <summary>
        /// Adds or updates the given model in the database
        /// depending on its state.
        /// </summary>
        /// <param name="model">The model to save</param>
        public async Task<MediaVM> Save(MediaVM model)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Create  Query For Media

                        var media = await _mediaRepository.Query()
                       .Include(m => m.Versions)
                       .FirstOrDefaultAsync(m => m.Id == model.Id)
                       .ConfigureAwait(false);

                    //var fileNameWithoutExt = model.Filename.Substring(0, model.Filename.LastIndexOf("."));

                    //model.Filename = String.Concat(fileNameWithoutExt, "(", SimilarNameCount.ToString(), ")", Path.GetExtension(model.Filename));
                    if (media == null)
                    {
                        media = new Media
                        {
                            CreationDate = DateTimeOffset.Now,
                            Filename = model.Filename
                        };


                        //Invoke Function If Have Any Duplicate FileNames And Return Boolean Value

                        var CheckFileNameDuplicate = await SimilarCountNamesInSameFolders(media.Filename,model.ContentType, model.FolderId, model.SiteId);

                        if (model.Force == false && CheckFileNameDuplicate)
                        {
                             throw new Exception("Duplicate File Name");
                           
                        }
                        else
                        {

                            //media.Filename

                            if (CheckFileNameDuplicate)
                            {
                                // delete the file by fileName 
                                var MediaToDelete = await _mediaRepository.Query().Where(x => x.Filename == model.Filename &&x.FolderId==model.FolderId&&x.SiteId==model.SiteId).FirstOrDefaultAsync().ConfigureAwait(false);

                                if (MediaToDelete != null)
                                {
                                
                                    _mediaRepository.Remove(MediaToDelete);
                               
                                }

                            }
                        }
                        _mediaRepository.Add(media);
                    }
                    //media.Filename = model.Filename;
                    media.FolderId = model.FolderId;
                    media.Type = model.Type;
                    media.Size = model.Size;
                    media.Width = model.Width;
                    media.Order = model.Order;
                    media.Height = model.Height;
                    media.ContentType = model.ContentType;
                    media.Properties = Media.SerializeProperties(model.Properties);
                    media.ModificationDate = DateTimeOffset.Now;
                    media.SiteId = model.SiteId;
                    media.PublicUrl = model.PublicUrl;
                    media.Status = model.Status;
                    // Delete removed versions

                    var current = model.Versions.Select(v => v.Id).ToArray();
                    var removed = media.Versions.Where(v => !current.Contains(v.Id)).ToArray();

                    if (removed.Length > 0)
                    {
                        for (int i = 0; i < removed.Length; i++)
                        {
                            _mediaversionsRepository.Remove(removed[i]);
                        }

                    }

                    // Add new versions
                    foreach (var version in model.Versions)
                    {
                        if (media.Versions.All(v => v.Id != version.Id))
                        {
                            var mediaVersion = new MediaVersion
                            {
                                //Id = version.Id,
                                MediaId = media.Id,
                                Size = version.Size,
                                Width = version.Width,
                                Height = version.Height,
                                FileExtension = version.FileExtension
                            };
                            _mediaversionsRepository.Add(mediaVersion);
                            media.Versions.Add(mediaVersion);
                        }
                    }

                    // Save all changes
                        await _mediaRepository.SaveChangesAsync().ConfigureAwait(false);
                        await transaction.CommitAsync();
                        return media;

                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw ex;

                    }
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds or updates the given model in the database
        /// depending on its state.
        /// </summary>
        /// <param name="model">The model</param>
        public async Task SaveFolder(MediaFolderVM model)
        {
            try
            {


                var folderList = await _mediafoldersRepository.Query()
                    .Where(x => x.SiteId == model.SiteId).Include(x => x.MediaFoldersDetails)
                    .ToListAsync()
                    .ConfigureAwait(false);
                var folder = new MediaFolders();

                folder = folderList.Where(f => f.MediaFoldersDetails.FirstOrDefault().CultureId == model.CultureId).FirstOrDefault();

                if (folder == null)
                {

                    folder = new MediaFolders()
                    {
                        // Id = model.Id != Guid.Empty ? model.Id : Guid.NewGuid(),
                        CreationDate = DateTimeOffset.Now,
                        ParentId = model.ParentId,
                        Order = model.Order
                    };
                    model.Id = folder.Id;
                    //if (folderList.Count() > 0)
                    //{
                    //    folder.Code = folderList.FirstOrDefault().Code;
                    //}
                    _mediafoldersRepository.Add(folder);

                    //  model.Id = folder.Id;

                }

                //folder.Description = model.Description;
                folder.ParentId = model.ParentId;
                //folder.Name = model.Name;
                //folder.Title = model.Title;
                //folder.SiteId = model.SiteId;
                //folder.CultureId = model.CultureId;
                //folder.CreatedById = model.CreatedById;

                await _mediafoldersRepository.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// Moves the media to the folder with the specified id.
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="folderId">The folder id</param>
        public async Task Move(MediaVM model, long? folderId, string? OldBreadCramb = null, string? NewBreadCramb = null,bool Force=false)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var media = await _mediaRepository.Query()
                        .FirstOrDefaultAsync(m => m.Id == model.Id)
                        .ConfigureAwait(false);

                    // If Media Null
                    if (media == null)
                    {
                        throw new Exception("File not found");
                    }
                    //After Check Now Will Change The Directory

                    //var OldPublicUrl = media.PublicUrl;

                    //var OldFileName = media.Filename;

                    //if(!string.IsNullOrEmpty(OldBreadCramb))
                    //{
                    //    OldFileName = OldFileName.Replace(OldBreadCramb.Replace(" ","_") + "-", "");

                    //}
                    //media.Filename = String.Concat(NewBreadCramb.Replace(" ", "_"), "-", OldFileName);

                    //if (OldBreadCramb.Length == 0)
                    //{
                    //    media.PublicUrl = OldPublicUrl.Substring(0, OldPublicUrl.LastIndexOf("/") + 1) + media.Filename;
                    //}

                    //else
                    //{
                    //    media.PublicUrl = OldPublicUrl.Replace(OldBreadCramb.Replace(" ","_"), NewBreadCramb.Replace(" ", "_"));
                    //}


                    media.FolderId = folderId;

                    var CheckFileNamesDuplicate = await SimilarCountNamesInSameFolders(media.Filename,model.ContentType, folderId, model.SiteId);

                    if (Force == false && CheckFileNamesDuplicate)
                    {
                        throw new Exception("Duplicate File Name");
                    }

                    else if(CheckFileNamesDuplicate)
                    {

                            // delete the file by fileName
                            var MediaToDelete = await _mediaRepository.Query().Where(x => x.Filename == media.Filename && x.FolderId == folderId && x.SiteId == model.SiteId).FirstOrDefaultAsync().ConfigureAwait(false);

                            if (MediaToDelete != null)
                            {

                                _mediaRepository.Remove(MediaToDelete);

                                File.Delete("wwwroot/" + media.PublicUrl);


                            }

                    }
                        await _mediaRepository.SaveChangesAsync().ConfigureAwait(false);
                        //MoveFileOrRename(OldPublicUrl, media.PublicUrl);
                        await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw ex;
                }
            });
        }
        /// <summary>
        /// Count The FileNames Have The Same Names In Same Folder And SiteId
        /// </summary>
        /// <param name="NewFileName"></param>
        /// <param name="folderId"></param>
        /// <param name="SiteId"></param>
        /// <returns></returns>

        private async Task<bool> SimilarCountNamesInSameFolders(string NewFileName,string ContentType,long?folderId,long?SiteId)
        {
          var CountNamesSimilar=  await _mediaRepository.Query().
                  Where(x => x.ContentType.Contains(ContentType) && x.FolderId == folderId &&
                              x.SiteId == SiteId &&
                              (x.Status == EStatus.Published || x.Status == EStatus.Unpublished)&&
                              x.Filename.ToLower() == NewFileName.ToLower())
                  .CountAsync()
                  .ConfigureAwait(false);

            if (CountNamesSimilar != 0)
                return true;
            return false;

        }
        /// <summary>
        /// Deletes the media with the given id.
        /// </summary>
        /// <param name="id">The unique id</param>
        public async Task Delete(long id)
        {
            try
            {
                var media = await _mediaRepository.Query()
                .Include(m => m.Versions)
                .FirstOrDefaultAsync(m => m.Id == id)
                .ConfigureAwait(false);

                if (media != null)
                {
                    _mediaRepository.Remove(media);
                    await _mediaRepository.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// Deletes the media folder with the given id.
        /// </summary>
        /// <param name="id">The unique id</param>
        public async Task DeleteFolder(long id)
        {
            var folder = await _mediafoldersRepository.Query()
                .Where(f => f.Id == id)
                .Include(f => f.MediaFoldersDetails)
                .Include(x=>x.FolderRoles)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            if (folder != null)
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        if (folder == null)
                        {
                            throw new Exception("Folder not exist");
                        }
                        if (folder.MediaFoldersDetails != null)
                        {

                            _mediafoldersDetailsRepository.RemoveRange(folder.MediaFoldersDetails);
                            await _mediafoldersDetailsRepository.SaveChangesAsync().ConfigureAwait(false);
                        }
                        if (folder.FolderRoles != null)
                        {

                            folderRoleRepository.RemoveRange(folder.FolderRoles);
                            await folderRoleRepository.SaveChangesAsync().ConfigureAwait(false);
                        }
                        _mediafoldersRepository.Remove(folder);
                        _mediafoldersRepository.SaveChanges();

                        await transaction.CommitAsync();
                    }
                    catch (Exception e)
                    {
                        await transaction.RollbackAsync();

                        throw new Exception("Could Not Delete Folder");
                    }
                });

            }
        }

        public async Task RetrieveFolder(long id)
        {
            var folder = await _mediafoldersRepository.Query()
                .Where(x=>x.Status == EStatus.Unpublished && x.Id == id)
                .Include(f => f.MediaFoldersDetails)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            if (folder != null)
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {

                        if (folder != null)
                        {

                                folder.Status = EStatus.Published;

                            await _mediafoldersRepository.SaveChangesAsync().ConfigureAwait(false);
                        }
                        await transaction.CommitAsync();
                    }
                    catch (Exception e)
                    {
                        await transaction.RollbackAsync();

                        throw new Exception("Could Not Delete Folder");
                    }
                });

            }
        }



        public async Task<bool> SoftDeleteFolder(List<long> foldersIds,long siteId)
        {
            var folders = await _mediafoldersRepository.Query()
                .Where(f => foldersIds.Contains(f.Id) && f.SiteId == siteId)
                .ToListAsync()
                .ConfigureAwait(false);

            if (folders.Any())
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                            foreach(var folder in folders)
                            {
                                folder.Status = EStatus.Unpublished;
                            }

                            await _mediafoldersRepository.SaveChangesAsync().ConfigureAwait(false);

                        await transaction.CommitAsync();
                    }
                    catch (Exception e)
                    {
                        await transaction.RollbackAsync();

                        throw new Exception("Could Not Delete Folder");
                    }
                });
                return true;

            }
            return true;
        }



        /// <summary>
        /// Sorts the items.
        /// </summary>
        /// <param name="folders">The full folder list</param>
        /// <param name="count">The list of item count</param>
        /// <param name="parentId">The current parent id</param>
        /// <param name="level">The current level in the structure</param>
        /// <returns>The structure</returns>
        private MediaStructure Sort(IEnumerable<MediaFolders> folders, IList<FolderCount> count, long cultureId, EStatus targetStatus, long? parentId = null, int level = 0)
        {
            var rootCount = count.FirstOrDefault(c => c.FolderId == null)?.Count;
            var totalCount = count.Sum(c => c.Count);
            var result = new MediaStructure
            {
                MediaCount = rootCount.HasValue ? rootCount.Value : 0,
                TotalCount = totalCount
            };

            var mediaFolders = folders as MediaFolders[] ?? folders.ToArray();
            foreach (var folder in mediaFolders.Where(f => f.ParentId == parentId )/*.OrderBy(f => f.Name)*/)
            {
                //var item = _mapper.Map<MediaFolders, MediaStructureItem>(folder);
                MediaStructureItem item = new MediaStructureItem()
                {
                    Id = folder.Id,
                    Order = folder.Order,
                    Status = folder.Status,
                    MediaFoldersDetails = folder.MediaFoldersDetails.ToList(),
                    Name = folder.MediaFoldersDetails.FirstOrDefault(x=> (cultureId !=0  ?  x.CultureId == cultureId : true))?.Title,
                    ThumbnailId = folder.MediaFoldersDetails.FirstOrDefault(x => (cultureId != 0 ? x.CultureId == cultureId : true))?.ThumbnailId,
                    ThumbnailMedia = folder.MediaFoldersDetails.FirstOrDefault(x => (cultureId != 0 ? x.CultureId == cultureId : true))?.Thumbnail,
                    Description = folder.MediaFoldersDetails.FirstOrDefault(x => (cultureId != 0 ? x.CultureId == cultureId : true))?.Description,
                };

                var folderCount = count.FirstOrDefault(c => c.FolderId == folder.Id)?.Count;

                item.Level = level;
                item.Items = Sort(mediaFolders, count, cultureId, targetStatus, folder.Id, level + 1);
                item.FolderCount = folders.Count(f => f.ParentId == item.Id);
                item.MediaCount = folderCount.HasValue ? folderCount.Value : 0;

               
                result.Add(item);
            }
            
            
            return result;
        }


        public MediaFolders GetFolderWithDetails(long folderId)
        {
            return _mediafoldersRepository.Query().Where(x => x.Id == folderId).Include(x => x.MediaFoldersDetails).FirstOrDefault();
        }

        public async Task SaveMediaDetails(MediaDetailsWithOrderVM model)
        {
            try
            {
                bool IsInEditMode = model.Id != 0;

                MediaDetails MediaDetails = new MediaDetails();

                if (IsInEditMode)
                {
                    MediaDetails = await _MediaDetailsRepository.Query()
                    .FirstOrDefaultAsync(m => m.Id == model.Id)
                    .ConfigureAwait(false);
                }


                MediaDetails.Title = model.Title;
                MediaDetails.AltText = model.AltText;
                MediaDetails.Description = model.Description;

                if (IsInEditMode)
                {
                    MediaDetails.ModifiedById = await _WorkContext.GetCurrentUserId();
                }
                else
                {
                    MediaDetails.CreatedById = await _WorkContext.GetCurrentUserId();
                    MediaDetails.CultureId = model.CultureId;
                    MediaDetails.MediaId = model.MediaId;
                    _MediaDetailsRepository.Add(MediaDetails);

                }

                var media = _mediaRepository.Query()
                                    .Where(m => m.Id == MediaDetails.MediaId)
                                    .FirstOrDefault();

                media.Order = model.Order;
                await _mediaRepository.SaveChangesAsync();

                // Save all changes
                await _MediaDetailsRepository.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> DeleteMediaWithDetails(List<long> mediaIds)
        {
            if (mediaIds == null || !mediaIds.Any())
                return true;

            var strategy = _context.Database.CreateExecutionStrategy();

            var filesToDelete = new HashSet<string>();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var allMedia = await _mediaRepository.Query()
                        .Where(m => mediaIds.Contains(m.Id))
                        .Include(m => m.MediaDetails)
                        .ToListAsync()
                        .ConfigureAwait(false);

                    if (allMedia.Any())
                    {
                        foreach (var media in allMedia)
                        {
                            var fileUrl = media.Status == EStatus.Unpublished
                                ? media.AltUrl
                                : media.PublicUrl;

                            if (!string.IsNullOrWhiteSpace(fileUrl))
                                filesToDelete.Add(fileUrl);

                            if (media.MediaDetails != null && media.MediaDetails.Any())
                            {
                                _MediaDetailsRepository.RemoveRange(media.MediaDetails);
                            }

                            var newGuid = Guid.NewGuid().ToString("N").Substring(0, 8);

                            media.Status = EStatus.Deleted;
                            media.Filename = $"{media.Filename}_{newGuid}";
                        }

                        await _context.SaveChangesAsync().ConfigureAwait(false);
                    }

                    await transaction.CommitAsync().ConfigureAwait(false);
                }
                catch
                {
                    await transaction.RollbackAsync().ConfigureAwait(false);
                    throw;
                }
            });

            // Delete physical files after DB commit.
            // File delete is not transactional, so do not do it inside the DB transaction/retry block.
            foreach (var fileUrl in filesToDelete)
            {
                try
                {
                    var relativePath = fileUrl.Replace("/", Path.DirectorySeparatorChar.ToString())
                                              .TrimStart(Path.DirectorySeparatorChar);

                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

                    if (File.Exists(fullPath))
                        File.Delete(fullPath);
                }
                catch
                {
                    // Optional: log the error here.
                    // Do not fail the API if DB deletion already succeeded.
                }
            }

            return true;
        }



        public async Task<bool> RetrieveMedia(long MediaId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var Transaction = await _context.Database.BeginTransactionAsync();
                var Media = await _mediaRepository.Query()
                    .Where(x=>x.Status == EStatus.Unpublished && x.Id == MediaId)
              .Include(m => m.Versions)
              .Include(m => m.MediaDetails)
              .FirstOrDefaultAsync()
              .ConfigureAwait(false);
                try
                {

                    if (Media != null && Media.MediaDetails != null)
                    {

                            Media.Status = EStatus.Published;
                            Media.PublicUrl = Media.AltUrl;
                            Media.AltUrl = null;
                            await _mediaRepository.SaveChangesAsync();

                    }
                    await Transaction.CommitAsync();
                    return true;

                }
                catch (Exception e)
                {
                    await Transaction.RollbackAsync();

                    throw e;

                }
            });

        }



        public async Task<bool> SoftDeleteMedia(List<long> mediaIds)
        {
            if (mediaIds == null || !mediaIds.Any())
                return true;

            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var medias = await _mediaRepository.Query()
                        .Where(m => mediaIds.Contains(m.Id))
                        .ToListAsync()
                        .ConfigureAwait(false);

                    if (medias.Any())
                    {
                        foreach (var media in medias)
                        {
                            media.Status = EStatus.Unpublished;
                            media.AltUrl = media.PublicUrl;
                            media.PublicUrl = "Resources/no-image.png";
                        }

                        await _context.SaveChangesAsync().ConfigureAwait(false);
                    }

                    await transaction.CommitAsync().ConfigureAwait(false);
                }
                catch
                {
                    await transaction.RollbackAsync().ConfigureAwait(false);
                    throw;
                }
            });

            return true;
        }
        public async Task<bool> updateThmbnail(MediaFolderVM mediaFolder)
        {
            try
            {
                var thumbnailFolder = await _mediafoldersDetailsRepository.Query().Where(x => x.FolderId == mediaFolder.Id).ToListAsync();
                foreach (var obj in thumbnailFolder)
                {
                    obj.ThumbnailId = mediaFolder.ThumbnailId;
                }
                _mediafoldersDetailsRepository.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        /// <summary>
        /// Move File From Source Location To Target Location
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="Target"></param>
        private void MoveFileOrRename(string Source, string Target)
        {
         

            File.Move("wwwroot/" + Source, "wwwroot/" + Target);
        }
    }
}
