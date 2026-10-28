
using System;
using System.Linq;
using System.Threading.Tasks;
//using Piranha.Models;
//using Piranha.Manager.Models;
using System.IO;
using System.Collections.Generic;
using System.Data;
using EskaCMS.Core.Services;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Models;
using EskaCMS.Entities.Entity;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Media.Models;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.ViewModels;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Extensions;
using EskaCMS.Media.Enums;
using EskaCMS.Core.Repositories;
using EskaCMS.Module.Core;
using System.Text;
using EskaCMS.Core.Runtime;
using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;
using System.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using EskaCMS.Entities.Entity.MediaEntity;
using Microsoft.AspNetCore.Identity;
using EskaCMS.Core.Cache;
using EskaCMS.Core.Services.Interface;
using System.Security.Policy;
using EskaCMS.DataAccess.Data;
using EskaCMS.Infrastructure.Web.SmartTable;
using Org.BouncyCastle.Asn1.X509;

namespace EskaCMS.Media.Services
{
    public class MediaService : Interfaces.IMediaService
    {
        //private readonly IApi _api;

        private readonly IMediaService _MediaService;
        private readonly IRepository<MediaFolders> _mediafoldersRepository;
        private readonly IRepository<MediaFoldersDetails> _mediafoldersdetailsRepository;
        private readonly IMediaRepository _repo;
        private readonly IWorkContext _WorkContext;
        private readonly HttpClient _HttpClient;
        private readonly RoleManager<Role> roleManager;
        private readonly IRepository<MediaRole> mediaRoleRepository;
        private readonly IRepository<FolderRole> folderRoleRepository;
        private readonly IStorage _storage;
        private readonly EskaDCMSDbContext context;
        private readonly IImageProcessor _processor;
        private readonly IRepository<EskaCMS.Entities.Entity.Core.Media> _mediaRepository;
        private static readonly object ScaleMutex = new object();
        private readonly string _basePath = "wwwroot/Resources/";
        private const string SystemAssetsFolderName = "SystemAssets";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="api">The default api</param>
        public MediaService(
            IMediaService MediaService,
            IRepository<MediaFolders> mediafoldersRepository,
            IRepository<MediaFoldersDetails> mediafoldersdetailsRepository,
            IWorkContext WorkContext,
            HttpClient HttpClient, RoleManager<Role> roleManager,
            IRepository<MediaRole> MediaRoleRepository,
            IRepository<FolderRole> FolderRoleRepository,
            IMediaRepository repo,
             IStorage storage,
                         EskaDCMSDbContext context,

             IRepository<EskaCMS.Entities.Entity.Core.Media> mediaRepository,
             IImageProcessor processor = null

        )
        {
            _MediaService = MediaService;
            _mediafoldersRepository = mediafoldersRepository;
            _mediafoldersdetailsRepository = mediafoldersdetailsRepository;
            _WorkContext = WorkContext;
            _HttpClient = HttpClient;
            this.roleManager = roleManager;
            mediaRoleRepository = MediaRoleRepository;
            folderRoleRepository = FolderRoleRepository;
            _repo = repo;
            _storage = storage;
            this.context = context;
            _processor = processor;
            _mediaRepository = mediaRepository;
            MediaManager.Init();
        }

        /// <summary>
        /// Get media model by media id
        /// </summary>
        /// <param name="id">Media id</param>
        /// <returns>Model</returns>
        public async Task<MediaListModel.MediaItem> GetById(long id)
        {




            var media = await GetByIdAsync(id);
            if (media == null)
                return null;

            return new Models.MediaListModel.MediaItem
            {
                Id = media.Id,
                FolderId = media.FolderId,
                Type = media.Type.ToString(),
                Filename = media.Filename,
                PublicUrl = media.PublicUrl.Replace("~", ""),
                ContentType = media.ContentType,
                Title = media.Title,
                AltText = media.AltText,
                Description = media.Description,
                Properties = media.Properties.ToArray().OrderBy(p => p.Key).ToList(),
                Size = FormatByteSize(media.Size),
                Order = media.Order,
                Width = media.Width,
                Height = media.Height,
                ModificationDate = media.ModificationDate
            };
        }

        public async Task<List<long>> GetChildFoldersId(long siteId, long id)
        {
            var allFolders = await _repo.GetAllFolders();
            var result = await _repo.GetAllChildrenFolders(siteId, id, allFolders);

            return result;
        }


        public async Task<long?> DeleteFoldersWithMedia(List<long> folderIds)
        {
            try
            {


                var siteId = _WorkContext.GetCurrentSiteId();
                var allFolders = await _repo.GetAllFolders();
                foreach (var parentFolderId in folderIds)
                {
                    var result = await _repo.GetAllChildrenFolders(siteId, parentFolderId, allFolders);



                    foreach (var folderId in result)
                    {
                        var allmedia = (await _repo.GetAllByMultibleFolders(siteId, new List<long> { folderId })).ToList();

                        await _repo.DeleteMediaWithDetails(allmedia);

                        await _repo.DeleteFolder(folderId);
                    }

                }


                return null;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<long?> SoftFoldersWithMedia(List<long> folderIds)
        {
            var siteId = _WorkContext.GetCurrentSiteId();
            var allFolders = await _repo.GetAllFolders();

            foreach (var parentFolderId in folderIds)
            {
                var result = await _repo.GetAllChildrenFolders(siteId, parentFolderId, allFolders);



                foreach (var folderId in result)
                {
                    var allmedia = (await _repo.GetAllByMultibleFolders(siteId, new List<long> { folderId })).ToList();

                    await SoftDeleteMedia(allmedia);

                }
                await _repo.SoftDeleteFolder(result, siteId);

            }

            return null;
        }

        public async Task<bool> RetrieveFolderWithMedia(RestoreFolders folders)
        {
            var siteId = _WorkContext.GetCurrentSiteId();

            var allFolders = await _repo.GetAllFolders();


            foreach (var Id in folders.FolderIds)
            {
                var result = await _repo.GetAllChildrenFolders(siteId, Id, allFolders);

                foreach (var folderId in result)
                {
                    var allmedia = await _repo.GetAllByMultibleFolders(siteId, new List<long> { folderId });
                    foreach (var media in allmedia)
                    {
                        await _repo.RetrieveMedia(media);
                    }
                    await _repo.RetrieveFolder(folderId);
                }

            }

            return true;
        }

        public async Task<bool> DeleteMultibleMedia(List<long> mediaIds)
        {


            await _repo.DeleteMediaWithDetails(mediaIds);


            return true;
        }

        public async Task<bool> SoftDeleteMedia(List<long> mediaIds)
        {

            await _repo.SoftDeleteMedia(mediaIds);


            return true;
        }

        public async Task<bool> RetrieveMultibleMedia(RestoreMediaVM model)
        {
            var folderIds = new List<long>();

            foreach (var folderId in model.FolderIds)
            {

                var parentsId = (await GetFolderBreadCrumb(folderId, null)).Where(x => !folderIds.Contains(x.Id)).Select(x => x.Id).ToList();
                folderIds.AddRange(parentsId);
            }
            foreach (var media in model.MediaIds)
            {
                await _repo.RetrieveMedia(media);
            }
            foreach (var id in folderIds)
            {
                await _repo.RetrieveFolder(id);
            }

            return true;
        }


        public async Task<MediaInfo> GetMediaPysicalFile(string URL)
        {
            var response = await _HttpClient.GetAsync(URL);
            if (response.IsSuccessStatusCode)
            {
                var contentType = response.Content.Headers.ContentType.ToString();
                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                return new MediaInfo(URL.Split('/').Last().Split('.').First(), fileBytes, contentType);
            }
            else
            {
                throw new Exception("remote server returned " + response.StatusCode);
            }
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private string FormatByteSize(double bytes)
        {
            string[] SizeSuffixes = { "bytes", "KB", "MB", "GB" };

            int index = 0;
            if (bytes > 1023)
            {
                do
                {
                    bytes /= 1024;
                    index++;
                } while (bytes >= 1024 && index < 3);
            }

            return $"{bytes:0.00} {SizeSuffixes[index]}";
        }
        /// <summary>
        /// Gets the breadcrumb list of the folders from the selected folder id
        /// </summary>
        /// <param name="structure">The complete media folder structure</param>
        /// <param name="folderId">The folder id</param>
        /// <returns></returns>
        public async Task<List<MediaFolderSimple>> GetFolderBreadCrumb(MediaStructure structure, long? folderId)
        {
            var folders = await GetFolderBreadCrumbReversed(structure, folderId);
            folders.Reverse();
            return folders;
        }


        public async Task<List<MediaFolderSimple>> GetFolderBreadCrumb(long? FolderId, long? ParentFolderId, EStatus? status = EStatus.Published)
        {
            var SiteId = await _WorkContext.GetCurrentSiteIdAsync();
            var MediaStructure = new Models.MediaListModel
            {
                CurrentFolderId = FolderId,
                ParentFolderId = ParentFolderId,
                Structure = await GetStructureAsync(SiteId, null, 0, status)

            };

            return await GetFolderBreadCrumb(MediaStructure.Structure, FolderId);




        }

        /// <summary>
        /// Gets the breadcrumb list of the folders from the selected folder id in reverse order with child first in list
        /// </summary>
        /// <param name="structure">The complete media folder structure</param>
        /// <param name="folderId">The folder id</param>
        /// <returns></returns>
        private async Task<List<MediaFolderSimple>> GetFolderBreadCrumbReversed(MediaStructure structure, long? folderId)
        {
            var folders = new List<MediaFolderSimple>();

            if (!folderId.HasValue)
                return folders;

            foreach (var item in structure)
            {
                if (item.Id == folderId)
                {
                    folders.Add(new MediaFolderSimple() { Id = item.Id, Name = item.Name });
                    return folders;
                }

                if (item.Items.Count > 0)
                {
                    folders = await GetFolderBreadCrumbReversed(item.Items, folderId);
                    if (folders.Count > 0)
                    {
                        folders.Add(new MediaFolderSimple() { Id = item.Id, Name = item.Name });
                        return folders;
                    }
                }
            }
            return folders;
        }

        /// <summary>
        /// Gets the list model for the specified folder, or the root
        /// folder if no folder id is given.
        /// </summary>
        /// <param name="folderId">The optional folder id</param>
        /// <param name="filter">The optional content type filter</param>
        /// <param name="width">The optional width for images</param>
        /// <param name="height">The optional height for images</param>
        /// <returns>The list model</returns>
        public async Task<Models.MediaListModel> GetList(long siteId, long? folderId = null,
            List<MediaType>? filter = null, int? width = null, int? height = null,
            int? PageSizefolder = null, int? PageNumberfolder = null, int? PageSizemedia = null, int? PageNumbermedia = null, string SearchText = null,
            EMediaFilter? orderFilter = null, EStatus? Status = EStatus.Published, List<string> ContentType = null)
        {
            var CultureId = _WorkContext.GetCurrentCultureId();
            var model = new Models.MediaListModel
            {
                CurrentFolderId = folderId,
                ParentFolderId = null,
                Structure = await GetStructureAsync(siteId, SearchText, CultureId, Status, orderFilter)
            };

            //model.Structure = FolderFilteration(model.Structure, SearchText);

            model.CurrentFolderBreadcrumb = await GetFolderBreadCrumb(model.Structure, folderId);

            model.RootCount = model.Structure.MediaCount;
            model.TotalCount = model.Structure.TotalCount;

            if (folderId.HasValue)
            {
                var partial = model.Structure.GetPartial(siteId, folderId, true);

                if (partial != null && (partial.FirstOrDefault()?.MediaCount == 0 && partial.FirstOrDefault()?.FolderCount == 0))
                {
                    model.CanDelete = true;
                }
            }

            if (folderId.HasValue)
            {
                var folder = await GetFolderByIdAsync(siteId, folderId.Value);
                if (folder != null)
                {
                    model.CurrentFolderName = folder.Title;
                    model.ParentFolderId = folder.ParentId;
                }
            }

            var holdMedia = (await GetAllByFolderIdAsync(siteId, folderId, SearchText, Status, ContentType));
            if (filter != null && filter.Any())
            {
                holdMedia = holdMedia
                    .Where(m => filter.Contains(m.Type));
            }
            var pairMedia = holdMedia.Select(m => new
            {
                media = m,
                mediaItem = new MediaListModel.MediaItem
                {
                    Id = m.Id,
                    FolderId = m.FolderId,
                    Type = m.Type.ToString(),
                    Filename = m.Filename,
                    PublicUrl = m.PublicUrl.TrimStart('~'), //Will only enumerate the start of the string, probably a faster operation.
                    ContentType = m.ContentType,
                    Title = m.MediaDetails.FirstOrDefault(x => x.CultureId == CultureId || CultureId == 0)?.Title,
                    AltText = m.MediaDetails.FirstOrDefault(x => x.CultureId == CultureId || CultureId == 0)?.AltText,
                    Description = m.MediaDetails.FirstOrDefault(x => x.CultureId == CultureId || CultureId == 0)?.Description,
                    Properties = m.Properties.ToArray().OrderBy(p => p.Key).ToList(),
                    Size = FormatByteSize(m.Size),
                    Order = m.Order,
                    Width = m.Width,
                    Height = m.Height,
                    ModificationDate = m.ModificationDate,
                    CultureId = m.CultureId,
                    Status = m.Status,
                    AltUrl = m.AltUrl
                }
            }).ToArray();


            //model.Structure =(MediaStructure) model.Structure
            //                                .Where(x => string.IsNullOrEmpty(SearchText)
            //                                            ||x.Name.ToLower().Contains(SearchText.ToLower())
            //                                           ||x.MediaFoldersDetails.Any(x=>x.Title.Contains(SearchText)));
            var MediaStructureItems = model.Structure.GetPartial(siteId, folderId)?.ToList();
            model.Folders = GetSpecificFolders(MediaStructureItems, SearchText, CultureId);
            model.TotalCountsFolder = model.Folders.Count();
            if (PageSizefolder.HasValue && PageNumberfolder.HasValue)
            {


                // default order from new to old 
                var query = model.Folders.OrderByDescending(x => x.Id);


                if (orderFilter.HasValue)
                {
                    switch (orderFilter)
                    {
                        case EMediaFilter.AscendingByOrder:
                            query = model.Folders.OrderBy(x => x.Order);
                            break;
                        case EMediaFilter.DescendingByOrder:
                            query = model.Folders.OrderByDescending(x => x.Order);
                            break;
                        case EMediaFilter.OldToNew:
                            query = model.Folders.OrderBy(x => x.Id);
                            break;
                    }
                }

                model.Folders = query.Skip((int)PageNumberfolder * (int)PageSizefolder)
                                  .Take((int)PageSizefolder)
                                  .ToList();



            }
            //model.Folders = model.Folders.GroupBy(x => x.Code).Select(x => x.FirstOrDefault()).ToList();

            if (width.HasValue)
            {
                foreach (var mp in pairMedia.Where(m => m.media.Type == MediaType.Image))
                {
                    if (mp.media.Versions.Any(v => v.Width == width && v.Height == height))
                    {
                        mp.mediaItem.AltVersionUrl =
                            (await EnsureVersionAsync(mp.media, width.Value, height).ConfigureAwait(false))
                            .TrimStart('~');
                    }
                }
            }


            model.Media = pairMedia
                            .Select(m => m.mediaItem)
                            .OrderByDescending(x => x.Id)
                            .ToList();
            model.TotalCountsMedia = model.Media.Count();
            if (PageNumbermedia.HasValue && PageSizemedia.HasValue)
            {
                // default order from new to old 
                var query = model.Media.OrderByDescending(x => x.Id);
                if (orderFilter.HasValue)
                {
                    switch (orderFilter)
                    {
                        case EMediaFilter.AscendingByOrder:
                            query = model.Media.OrderBy(x => x.Order);
                            break;
                        case EMediaFilter.DescendingByOrder:
                            query = model.Media.OrderByDescending(x => x.Order);
                            break;
                        case EMediaFilter.OldToNew:
                            query = model.Media.OrderBy(x => x.Id);
                            break;
                    }
                }

                model.Media = query.Skip((int)PageNumbermedia * (int)PageSizemedia)
                                  .Take((int)PageSizemedia)
                                  .ToList();
            }
            model.ViewMode = model.Media.Count(m => m.Type == "Image") > model.Media.Count / 2 ? MediaListModel.GalleryView : MediaListModel.ListView;
            return model;
        }

        private IList<MediaListModel.FolderItem> GetSpecificFolders(
            List<MediaStructureItem> mediaStructureItems, string searchText, long? CultureId)
        {
            //var result = new List<MediaListModel.FolderItem>();

            //if (mediaStructureItems == null)
            //    return result;

            //foreach (var folder in mediaStructureItems)
            //{
            //    if (string.IsNullOrEmpty(searchText) ||
            //        folder.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            //        (folder.MediaFoldersDetails?.Any(d =>
            //            !string.IsNullOrEmpty(d.Title) &&
            //            d.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ?? false))
            //    {
            //        result.Add(MapFolder(folder, CultureId));
            //    }

            //    // recurse into children
            //    if (folder.Items != null && folder.Items.Any())
            //    {
            //        result.AddRange(GetSpecificFolders(folder.Items, searchText, CultureId));
            //    }
            //}

            //return result;
            try
            {
                var Result = new List<MediaListModel.FolderItem>();
                if (mediaStructureItems != null)
                {
                    Result = mediaStructureItems
                     .Where(x => string.IsNullOrEmpty(searchText) || (x.Name.ToLower().Contains(searchText.ToLower()) || x.Items.Any(x => x.Name.ToLower().Contains(searchText)))).Select(f => new MediaListModel.FolderItem
                     {
                         Id = f.Id,
                         Name = f.MediaFoldersDetails.FirstOrDefault(x => x.CultureId == CultureId || CultureId == 0)?.Title,
                         ItemCount = f.MediaCount,
                         FolderCount = f.FolderCount,
                         Order = f.Order,
                         ThumbnailUrl = f.MediaFoldersDetails.FirstOrDefault(x => x.CultureId == CultureId || CultureId == 0)?.Thumbnail?.PublicUrl,
                         Status = f.Status
                     }).ToList();
                }
                return Result;


            }
            catch (Exception ex)
            {

                throw ex;
            }

        
           
        }


        //private IList<MediaFolders> FolderFilteration(MediaStructure media, string searchText = null)
        //{
        //    try
        //    {

        //        if (!string.IsNullOrEmpty(searchText))
        //        {

        //            // var filtered = await folders
        //            //.Where(x => x.MediaFoldersDetails.Any(y => y.Title.ToLower().Contains(search.ToLower())
        //            //    || x.ChildFolders.Any(c => c.MediaFoldersDetails.Any(d => d.Title.ToLower().Contains(search.ToLower())))))
        //            // .ToListAsync();
        //            var filtered = media
        //                .Where(x => x.Items.Any(y => y.MediaFoldersDetails.Any(y => y.Title.ToLower().Contains(searchText.ToLower())))).ToList();

        //            //walk up the tree to include parents
        //            var finalList = new List<MediaStructure> filtered;
        //            foreach (var f in filtered)
        //            {
        //                //if a parent matches check the children
        //                if (f.MediaFoldersDetails.Any(d => d.Title.ToLower().Contains(searchText.ToLower())))
        //                {
        //                    if (!filtered.Any(x => x.Id == f.Id))
        //                    {
        //                        finalList.Add(f);
        //                        f.ChildFolders = f.ChildFolders?
        //                            .Where(c => c.MediaFoldersDetails.Any(d => d.Title.ToLower().Contains(searchText.ToLower())))
        //                            .ToList();
        //                    }
        //                }
        //                else
        //                {    //if a child matches but not parent so include the parent and children that only matches
        //                    var parent = folders.FirstOrDefault(p => p.Id == f.ParentId);
        //                    if (parent != null)
        //                    {
        //                        var parentCopy = new MediaFolders
        //                        {
        //                            Id = parent.Id,
        //                            ParentId = parent.ParentId,
        //                            Status = parent.Status,
        //                            MediaFoldersDetails = parent.MediaFoldersDetails,
        //                            FolderRoles = parent.FolderRoles,
        //                            ChildFolders = parent.ChildFolders
        //                                ?.Where(c => c.MediaFoldersDetails.Any(d => d.Title.ToLower().Contains(searchText.ToLower())))
        //                                .GroupBy(x => x.Id) //to remove duplicate
        //                                .Select(g => g.First())
        //                                .ToList()
        //                        };

        //                        if (!finalList.Any(x => x.Id == parentCopy.Id))
        //                            finalList.Add(parentCopy);
        //                    }
        //                }
        //            }
        //            result = finalList.ToList();

        //        }
        //        return result;

        //    }
        //    catch (Exception ex)
        //    {
        //    }
        //}

        private MediaListModel.FolderItem MapFolder(MediaStructureItem f, long? CultureId) =>
            new MediaListModel.FolderItem
            {
                Id = f.Id,
                Name = f.MediaFoldersDetails
                    .FirstOrDefault(x => x.CultureId == CultureId || CultureId == 0)?.Title,
                ItemCount = f.MediaCount,
                FolderCount = f.FolderCount,
                Order = f.Order,
                ThumbnailUrl = f.MediaFoldersDetails
                    .FirstOrDefault(x => x.CultureId == CultureId || CultureId == 0)?.Thumbnail?.PublicUrl,
                Status = f.Status
            };
        // Recursive Fun to check child folders
        private bool LookUpFolderByName(MediaFolders folder,string searchText)
        {
         
                if (folder == null) return false;

                // check current folder details
                if (folder.MediaFoldersDetails != null &&

                    folder.MediaFoldersDetails.Any(d =>

                        !string.IsNullOrEmpty(d.Title) &&
                        
                        d.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase)))

                {

                    return true;

                }



                // recursively check child folders

                if (folder.ChildFolders != null &&

                    folder.ChildFolders.Any(cf => LookUpFolderByName(cf, searchText)))

                {

                    return true;

                }



                return false;

            
        }
        //public async Task SaveFolder(MediaFolderVM model)
        //{
        //    var folderList = await _MediaService.GetByFolderNameAsync(model.Code, model.SiteId);
        //    if (folderList.Count() > 0)
        //    {
        //        var folder = folderList.Where(x => x.CultureId == model.CultureId).Select(x => new MediaFolderVM
        //        {
        //            Id = x.Id,
        //            ParentId = x.ParentId,
        //            Title = x.Title,
        //            Description = x.Description,
        //            Name = x.Name,
        //            CultureId = x.CultureId,
        //            Code = x.Code,
        //            CreatedById = x.CreatedById
        //        }).FirstOrDefault();

        //        if (folder == null)
        //        {
        //            folder = folderList.Select(x => new MediaFolderVM
        //            {
        //                Id = x.Id,
        //                ParentId = x.ParentId,
        //                Title = x.Title,
        //                Description = x.Description,
        //                Name = x.Name,
        //                CultureId = x.CultureId,
        //                Code = x.Code
        //            }).FirstOrDefault();
        //            folder.Code = model.Name;
        //            folder.CreatedById = model.CreatedById;
        //        }
        //        folder.Name = model.Name;
        //        folder.Title = model.Title;
        //        folder.CultureId = model.CultureId;
        //        folder.Description = model.Description;
        //        folder.SiteId = model.SiteId;
        //        folder.CreatedById = model.CreatedById;
        //        folder.ParentId = model.ParentId;
        //        await _MediaService.SaveFolderAsync(folder);
        //    }
        //    else
        //    {
        //        await _MediaService.SaveFolderAsync(model);
        //    }
        //}

        public async Task SaveFolder(RequestMediaFolderVM model)
        {
            var strategy = context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var DBTrans = await context.Database.BeginTransactionAsync();
                try
                {
                    var siteId = _WorkContext.GetCurrentSiteId();
                    var createdById = await _WorkContext.GetCurrentUserId();
                    var folder = GetFolderWithDetails(model.Id);

                    if (folder == null)
                    {
                        await CreateNewFolder(model, siteId, createdById);
                    }
                    else
                    {
                        await UpdateExistingFolder(model, folder, createdById);
                    }
                    await DBTrans.CommitAsync();

                }
                catch (Exception ex)
                {
                    await DBTrans.RollbackAsync();
                    throw ex;
                }
            });
        }

        private async Task CreateNewFolder(RequestMediaFolderVM model, long siteId, long createdById)
        {
            try
            {
                var mediaFolder = new MediaFolders
                {
                    ParentId = model.ParentId,
                    SiteId = siteId,
                    Order = model.Order,
                    Status = EStatus.Published
                };

                _mediafoldersRepository.Add(mediaFolder);
                await _mediafoldersRepository.SaveChangesAsync();

                var mediaFolderDetailsList = new List<MediaFoldersDetails>();

                foreach (var folderDetail in model.MediaFoldersDetails)
                {
                    if (await IsDuplicateFolderName(folderDetail, mediaFolder.Id, model.ParentId))
                    {
                        throw new Exception($"The folder name {folderDetail.Title} already exists. Please choose a different name and try again.");
                    }

                    mediaFolderDetailsList.Add(new MediaFoldersDetails
                    {
                        FolderId = mediaFolder.Id,
                        ThumbnailId = folderDetail.ThumbnailId,
                        Title = folderDetail.Title,
                        Description = folderDetail.Description,
                        CultureId = folderDetail.CultureId,
                        CreatedById = createdById,
                        CreationDate = DateTime.Now
                    });
                }

                _mediafoldersdetailsRepository.AddRange(mediaFolderDetailsList);
                await _mediafoldersdetailsRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Handle or log the exception specific to folder creation
                throw new ApplicationException("An error occurred while creating the folder.", ex);
            }
        }

        private async Task UpdateExistingFolder(RequestMediaFolderVM model, MediaFolders folder, long createdById)
        {
            try
            {
                folder.Order = model.Order;
                await _mediafoldersRepository.SaveChangesAsync();

                foreach (var mediaFolderDetail in folder.MediaFoldersDetails)
                {
                    var updatedDetail = model.MediaFoldersDetails
                        .Where(x => x.CultureId == mediaFolderDetail.CultureId).FirstOrDefault();

                    if (updatedDetail != null)
                    {
                        if (await IsDuplicateFolderName(updatedDetail, folder.Id, model.ParentId))
                        {
                            throw new Exception("The folder name already exists. Please choose a different name and try again.");
                        }

                        mediaFolderDetail.ThumbnailId = updatedDetail.ThumbnailId.HasValue ? updatedDetail.ThumbnailId : null;
                        mediaFolderDetail.Title = updatedDetail.Title;
                        mediaFolderDetail.ModifiedById = createdById;
                        mediaFolderDetail.Description = updatedDetail.Description;
                    }
                }

                await _mediafoldersdetailsRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<bool> IsDuplicateFolderName(RequestMediaFoldersDetails folderDetail, long folderId, long? parentId = null)
        {
            try
            {
                return await _mediafoldersdetailsRepository.Query()
                    .Include(detail => detail.Folder)
                    .Where(x => x.FolderId != folderId)
                    .Where(detail => detail.CultureId == folderDetail.CultureId && detail.Title.ToLower() == folderDetail.Title.ToLower())
                    .Where(detail => detail.Folder.ParentId == parentId)
                    .CountAsync() > 0;
            }
            catch (Exception ex)
            {
                // Handle or log any exceptions related to duplicate check
                throw new ApplicationException("An error occurred while checking for duplicate folder names.", ex);
            }
        }
        public async Task<long?> DeleteFolder(long siteId, long id)
        {
            var folder = await GetFolderByIdAsync(siteId, id);

            if (folder != null)
            {
                folder.SiteId = siteId;
                await DeleteFolderAsync(folder);
                return folder.ParentId;
            }
            return null;
        }

        public async Task<bool> MoveFolders(TransferFolderVm model)
        {
            var siteId = _WorkContext.GetCurrentSiteId();
            var currentFolders = _mediafoldersRepository.Query()
                                                        .Where(x => model.CurrentFolderIds.Contains(x.Id) && x.SiteId == siteId && x.Status == EStatus.Published)
                                                        .ToList();



            if (currentFolders.Any())
            {
                if (currentFolders.Any(o => o.ParentId == model.TargetFolderId))
                {
                    throw new Exception("One or more of the selected folders are already in the target folder. Please choose different folders to move.");
                }

                foreach (var folder in currentFolders)
                {
                    folder.ParentId = model.TargetFolderId;
                }

                _mediafoldersRepository.SaveChanges();
                return true;
            }
            return false;
        }


        /// <summary>
        /// Save or update media assets to storage
        /// </summary>
        /// <param name="model">Upload model</param>
        /// <returns>The number of upload managed to be saved or updated</returns>
        public async Task<List<MediaVM>> SaveMedia(MediaUploadModel model)
        {
            try
            {
                long SiteId = await _WorkContext.GetCurrentSiteIdAsync();
                var MediaStructure = new Models.MediaListModel
                {
                    CurrentFolderId = model.ParentId,
                    ParentFolderId = null,
                    Structure = await GetStructureAsync(SiteId)

                };

                var CurrentFolderBreadcrumb = await GetFolderBreadCrumb(MediaStructure.Structure, model.ParentId);


                var uploadedMediaIds = new List<MediaVM>();

                string folderPath = "";

                foreach (var item in CurrentFolderBreadcrumb)
                {
                    folderPath += folderPath += $"\\{item.Name}";
                }

                // Go through all of the uploaded files
                foreach (var upload in model.Uploads)
                {




                    if (!string.IsNullOrWhiteSpace(upload.ContentType))
                    {
                        using (var stream = upload.OpenReadStream())
                        {
                            if (model.ConvertToWebp.Value && MediaManager.GetMediaType(upload.FileName) == MediaType.Image && (!MediaManager.GetContentType(upload.FileName).Contains("image/webp") && !MediaManager.GetContentType(upload.FileName).Contains("image/x-icon")))
                            {

                                if (MediaManager.GetContentType(upload.FileName).Contains("image/svg+xml"))
                                {
                                    throw new Exception("You cannot convert from svg to webp");
                                }

                                using (var image = await SixLabors.ImageSharp.Image.LoadAsync(stream))
                                {

                                    using (var webpStream = new MemoryStream())
                                    {

                                        image.SaveAsWebp(webpStream);


                                        webpStream.Position = 0;


                                        var mediaIds = await SaveAsync(new StreamMediaContent
                                        {
                                            Id = model.Uploads.Count() == 1 ? model.Id : null,
                                            FolderId = model.ParentId,
                                            Filename = $"{Path.GetFileNameWithoutExtension(upload.FileName)}.webp",//Path.GetFileName(upload.FileName)
                                            Data = webpStream,
                                            CreatedById = model.CreatedBy,
                                            SiteId = model.SiteId,
                                            CultureId = model.CultureId,
                                            Force = model.Force.HasValue ? model.Force.Value : false,
                                            FolderPath = folderPath,
                                            Status = model.status


                                        });

                                        uploadedMediaIds.AddRange(mediaIds);
                                    }
                                }
                            }
                            else
                            {

                                var mediaIds = await SaveAsync(new StreamMediaContent
                                {
                                    Id = model.Uploads.Count() == 1 ? model.Id : null,
                                    FolderId = model.ParentId,
                                    Filename = Path.GetFileName(upload.FileName),
                                    Status = model.status,
                                    Data = stream,
                                    CreatedById = model.CreatedBy,
                                    SiteId = model.SiteId,
                                    CultureId = model.CultureId,
                                    Force = model.Force.HasValue ? model.Force.Value : false,
                                    FolderPath = folderPath,


                                });

                                uploadedMediaIds.AddRange(mediaIds);
                            }
                        }
                    }
                }
                return uploadedMediaIds;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Saves the updated meta information for the given media asset.
        /// </summary>
        /// <param name="media">The media asset</param>
        /// <returns>If the meta information was updated successful</returns>
        public async Task<bool> SaveMeta(MediaListModel.MediaItem media)
        {
            try
            {

                var mediaDetailsList = await GetMediaDetailsAsync(media.Id, media.CultureId);

                var model = mediaDetailsList.FirstOrDefault();

                if (model == null)
                {
                    model = new Core.ViewModels.MediaDetailsWithOrderVM();
                    model.CultureId = media.CultureId;
                    model.Id = 0;
                    model.MediaId = media.Id;
                }

                model.Title = media.Title;
                model.AltText = media.AltText;
                model.Description = media.Description;
                model.Order = media.Order;

                await SaveMediaDetailsAsync(model);

                return true;

            }
            catch (Exception E)
            {

                return false;
            }

        }

        public async Task<MediaDetailsWithOrderVM> GetMeta(long mediaId, long siteId, long cultureId = 0)
        {
            var mediaList = await GetMediaDetailsAsync(mediaId, cultureId);
            return mediaList.Where(y => y.CultureId == cultureId).FirstOrDefault();
        }
        public async Task<MediaFolderVM> GetFolderByCulture(string folderCode, long siteId, long cultureId)
        {
            var mediaFolderList = await GetByFolderNameAsync(folderCode, siteId);
            return mediaFolderList.Where(x => x.CultureId == cultureId).FirstOrDefault();
        }


        public async Task<MediaFolderDetailsWithOrderVM> GetFolderDetailsByCultureId(long CultureId, long FolderId, long siteId)
        {
            var result = await GetFoldersDetailsByCultureId(FolderId, CultureId, siteId);
            return result;
        }


        public async Task<long?> DeleteMedia(long id)
        {

            await DeleteMediaWithDetails(id);
            return id;

            return null;
        }

        public MediaFolders GetFolderByCulture(long folderId, long cultureId)
        {
            var folder = _mediafoldersRepository.Query().Include(x => x.MediaFoldersDetails)
                .Where(x => x.Id == folderId).FirstOrDefault();
            return folder;
        }

        public MediaFolders GetFolderByTitle(string title, long siteId, long? folderParentId)
        {
            var folder = _mediafoldersdetailsRepository.Query()
                                                       .Where(x => x.Title.ToLower() == title.ToLower() && x.Folder.SiteId == siteId && (folderParentId.HasValue ? x.Folder.ParentId == folderParentId.Value : true))
                                                       .Include(x => x.Folder)
                                                       .Select(x => x.Folder)
                                                       .FirstOrDefault();
            return folder;
        }


        public async Task<FileExtension> GetFileExtensionFromBase64(CreateFileVM model)
        {
            var file = FileHelper.GetFileExtension(model.File_Base64);

            return file;
        }

        public async Task<MediaStructure> GetStructureAsync(long siteId, string search = null, long CultureId = 0, EStatus? Status = EStatus.Published, EMediaFilter? orderFilter = EMediaFilter.NewToOld)
        {


            var structure = await _repo.GetStructure(siteId, search, CultureId, Status, orderFilter).ConfigureAwait(false);


            return structure;
        }
        //public async Task<MediaStructure> GetStructureAsync(long siteId, long CultureId = 0 , EStatus? Status = EStatus.Published)
        //{

        //    var structure = await _repo.GetStructure(siteId, CultureId,Status).ConfigureAwait(false);


        //    return structure;
        //}


        /// <summary>
        /// Gets the media with the given id.
        /// </summary>
        /// <param name="id">The unique id</param>
        /// <returns>The media</returns>
        public async Task<MediaVM> GetByIdAsync(long id)
        {
            //  var model = _cache?.Get<Core.Entities.Media>(id.ToString());

            // if (model == null)
            {
                var model = await _repo.GetById(id).ConfigureAwait(false);

                OnLoad(model);
                //  }
                return model;
            }
        }

        private void OnLoad(MediaVM model)
        {
            if (model != null)
            {
                // Get public url
                if (model.Status == EStatus.Published)
                {
                    model.PublicUrl = GetPublicUrl(model);

                }

                //Create missing properties
                foreach (var key in MediaManager.MetaProperties)
                {
                    if (!model.Properties.Any(p => p.Key == key))
                    {
                        model.Properties.Add(key, null);
                    }
                }

            }
        }

        /// <summary>
        /// Gets the public url for the given media.
        /// </summary>
        /// <param name="media">The media object</param>
        /// <param name="width">Optional requested width</param>
        /// <param name="height">Optional requested height</param>
        /// <param name="extension">Optional requested extension</param>
        /// <returns>The name</returns>
        private string GetPublicUrl(MediaVM media, int? width = null, int? height = null, string extension = null)
        {
            try
            {
                var name = GetResourceName(media, width, height, extension);


                var cdn = "Resources/";

                if (!string.IsNullOrWhiteSpace(cdn))
                {
                    return cdn + _storage.GetResourceName(media, name);
                }

                return _storage.GetPublicUrl(media, name);
            }
            catch (Exception ex)
            {

                throw ex;
            }


        }

        /// <summary>
        /// Gets the public url for the given media.
        /// </summary>
        /// <param name="media">The media object</param>
        /// <param name="width">Optional requested width</param>
        /// <param name="height">Optional requested height</param>
        /// <param name="extension">Optional requested extension</param>
        /// <returns>The name</returns>
        private string NewGetPublicUrl(MediaVM media, int? width = null, int? height = null, string extension = null)
        {
            try
            {
                var name = GetResourceName(media, width, height, extension);

                EnsureDirectoryExist("ste", 2);
                var cdn = "Resources/";

                if (!string.IsNullOrWhiteSpace(cdn))
                {
                    return cdn + _storage.GetResourceName(media, name);
                }

                return _storage.GetPublicUrl(media, name);
            }
            catch (Exception ex)
            {

                throw ex;
            }


        }

        private bool EnsureDirectoryExist(string folderPath, long siteId)
        {
            try
            {

                var FolderName = "wwwroot\\Resources\\site_" + siteId + folderPath;

                if (!Directory.Exists(FolderName))
                {
                    Directory.CreateDirectory(FolderName);

                }
                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }


        }

        /// <summary>
        /// Gets the media resource name.
        /// </summary>
        /// <param name="media">The media object</param>
        /// <param name="width">Optional requested width</param>
        /// <param name="height">Optional requested height</param>
        /// <param name="extension">Optional requested extension</param>
        /// <returns>The name</returns>

        private string GetResourceName(MediaVM media, int? width = null, int? height = null, string extension = null)
        {
            try
            {
                var filename = new FileInfo(media.Filename);
                var sb = new StringBuilder();

                //
                // This is now handled in the provider
                //
                // sb.Append(media.Id);
                // sb.Append("-");
                //

                if (width.HasValue)
                {
                    if (!string.IsNullOrEmpty(filename.Extension))
                    {
                        sb.Append(filename.Name.Replace(filename.Extension, "_"));

                    }
                    else
                    {
                        sb.Append(filename.Name);
                    }
                    sb.Append(width);

                    if (height.HasValue)
                    {
                        sb.Append("x");
                        sb.Append(height.Value);
                    }
                    else
                    {
                        sb.Append(filename.Name);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(filename.Extension))
                        sb.Append(filename.Name.Replace(filename.Extension, ""));
                }

                if (string.IsNullOrEmpty(extension))
                {

                    sb.Append(filename.Extension);
                }
                else
                {
                    sb.Append(extension);
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        /// <summary>
        /// Gets the media folder with the given id.
        /// </summary>
        /// <param name="id">The unique id</param>
        /// <returns>The media folder</returns>
        public async Task<MediaFolderVM> GetFolderByIdAsync(long siteId, long id)
        {

            var model = await _repo.GetFolderById(siteId, id).ConfigureAwait(false);


            return model;
        }

        /// <summary>
        /// Gets all media available in the specified folder.
        /// </summary>
        /// <param name="folderId">The optional folder id</param>
        /// <returns>The available media</returns>
        public Task<IEnumerable<MediaVM>> GetAllByFolderIdAsync(long siteId, long? folderId = null, string SearchText = null, EStatus? Status = EStatus.Published, List<string> ContetntType = null)
        {
            try
            {
                return _repo.GetAll(siteId, folderId, SearchText, Status, ContetntType).ContinueWith(t => _getFast(t.Result.ToArray())).Unwrap();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        //Separated this into its own thing in case it needed to get reused elsewhere.
        private async Task<IEnumerable<MediaVM>> _getFast(IEnumerable<long> ids)
        {
            try
            {
                var guids = ids as long[] ?? ids.ToArray();
                var partial = Enumerable.Empty<MediaVM>().Where(c => c != null).ToArray();
                var missingIds = guids.Except(partial.Select(c => c.Id)).ToArray();
                var returns = partial.Concat((await _repo.GetById(missingIds)).Select(c =>
                {
                    OnLoad(c);
                    return c;
                })).OrderBy(m => m.Id).ToArray();
                return returns;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        /// <summary>
        /// Ensures that the image version with the given size exsists
        /// and returns its public URL.
        /// </summary>
        /// <param name="id">The unique id</param>
        /// <param name="width">The requested width</param>
        /// <param name="height">The optionally requested height</param>
        /// <returns>The public URL</returns>
        public async Task<string> EnsureVersionAsync(long id, int width, int? height = null)
        {
            var media = await GetByIdAsync(id).ConfigureAwait(false);

            return media != null ? await EnsureVersionAsync(media, width, height).ConfigureAwait(false) : null;
        }

        public async Task<string> EnsureVersionAsync(MediaVM media, int width, int? height = null)
        {
            // If no processor is registered, return the original url
            if (_processor == null)
                return GetPublicUrl(media);

            // Get the media type
            var type = MediaManager.GetItem(media.Filename);

            // If this type doesn't allow processing, return the original url
            if (!type.AllowProcessing)
                return GetPublicUrl(media);

            // If the requested size is equal to the original size, return true
            if (media.Width == width && (!height.HasValue || media.Height == height.Value))
                return GetPublicUrl(media);

            var query = media.Versions
                .Where(v => v.Width == width);

            query = height.HasValue ? query.Where(v => v.Height == height) : query.Where(v => !v.Height.HasValue);

            var version = query.FirstOrDefault();

            if (version != null)
                return media.Width == width && (!height.HasValue || media.Height == height.Value)
                    ? GetPublicUrl(media)
                    : GetPublicUrl(media, width, height, version.FileExtension);

            // Get the image file
            using (var stream = new MemoryStream())
            {
                using (var session = await _storage.OpenAsync().ConfigureAwait(false))
                {
                    if (!await session.GetAsync(media, media.Filename, stream).ConfigureAwait(false))
                    {
                        return null;
                    }

                    // Reset strem position
                    stream.Position = 0;

                    using (var output = new MemoryStream())
                    {
                        if (height.HasValue)
                        {
                            _processor.CropScale(stream, output, width, height.Value);
                        }
                        else
                        {
                            _processor.Scale(stream, output, width);
                        }
                        output.Position = 0;
                        bool upload = false;

                        lock (ScaleMutex)
                        {
                            // We have to make sure we don't scale multiple files
                            // at the same time as it can create index violations.
                            version = query.FirstOrDefault();

                            if (version == null)
                            {
                                var info = new FileInfo(media.Filename);

                                version = new MediaVersion
                                {

                                    Size = output.Length,
                                    Width = width,
                                    Height = height,
                                    FileExtension = info.Extension
                                };
                                media.Versions.Add(version);

                                _repo.Save(media).Wait();


                                upload = true;
                            }
                        }

                        if (upload)
                        {
                            await session.PutAsync(media, GetResourceName(media, width, height), media.ContentType,
                                    output).ConfigureAwait(false);

                            var info = new FileInfo(media.Filename);
                            return GetPublicUrl(media, width, height, info.Extension);
                        }
                        //When moving this out of its parent method, realized that if the mutex failed, it would just fall back to the null instead of trying to return the issue.
                        //Added this to ensure that queries didn't just give up if they weren't the first to the party.
                        return GetPublicUrl(media, width, height, version.FileExtension);
                    }
                }
            }
            // If the requested size is equal to the original size, return true
        }

        public MediaFolders GetFolderWithDetails(long folderId)
        {
            return _repo.GetFolderWithDetails(folderId);
        }

        /// <summary>
        /// Deletes the given model.
        /// </summary>
        /// <param name="model">The media</param>
        public Task DeleteFolderAsync(MediaFolderVM model)
        {
            return DeleteFolderAsync(model.SiteId, model.Id);
        }

        /// <summary>
        /// Deletes the media folder with the given id.
        /// </summary>
        /// <param name="id">The unique id</param>
        public async Task DeleteFolderAsync(long siteId, long id)
        {
            var folder = await GetFolderByIdAsync(siteId, id).ConfigureAwait(false);

            if (folder != null)
            {



                await _repo.DeleteFolder(id).ConfigureAwait(false);

            }
        }

        /// <summary>
        /// Updates the meta data for the given media model.
        /// </summary>
        /// <param name="model">The model</param>
        public async Task SaveAsync(MediaVM model)
        {
            // Make sure we have an existing media model with this id.
            var current = await GetByIdAsync(model.Id);

            if (current == null)
            {
                current = model;
            }
            // Validate model
            var context = new ValidationContext(model);
            Validator.ValidateObject(model, context, true);


            await _repo.Save(model).ConfigureAwait(false);
            //else
            //{
            //    //throw new FileNotFoundException("You can only update meta data for an existing media object");
            //}
        }

        /// <summary>
        /// Adds or updates the given model in the database
        /// depending on its state.
        /// </summary>
        /// <param name="content">The content to save</param>
        public async Task<List<MediaVM>> SaveAsync(MediaContent content)
        {
            try
            {
                // Setup media types


                if (!MediaManager.IsSupported(content.Filename))
                {
                    throw new ValidationException("Filetype not supported.");
                }

                List<EskaCMS.Entities.Entity.Core.Media> mediaList = new List<EskaCMS.Entities.Entity.Core.Media>();
                var oldMedia = new EskaCMS.Entities.Entity.Core.Media();
                if (content.Id.HasValue)
                {
                    oldMedia = await GetByIdAsync(content.Id.Value).ConfigureAwait(false);
                    mediaList = await GetByFileNameAsync(oldMedia.Filename, content.SiteId).ConfigureAwait(false);
                }
                MediaVM model = new MediaVM();
                var mediaIds = new List<MediaVM>();

                if (mediaList.Count() == 0)
                {
                    model = new MediaVM()
                    {
                        CreatedById = content.CreatedById,
                        SiteId = content.SiteId,
                        CreationDate = DateTimeOffset.Now
                    };

                    var media = await SaveUpdatMedia(content);

                    // added to handle add media for web
                    mediaIds.Add(media);
                }
                else
                {
                    using (var session = await _storage.OpenAsync().ConfigureAwait(false))
                    {
                        foreach (var media in mediaList)
                        {
                            // Delete all versions as we're updating the image
                            if (media.Versions.Count > 0)
                            {
                                foreach (var version in media.Versions)
                                {
                                    // Delete version from storage
                                    await session.DeleteAsync(media, GetResourceName(media, version.Width, version.Height, version.FileExtension)).ConfigureAwait(false);
                                }
                                media.Versions.Clear();
                            }

                            // Delete the old file because we might have a different filename
                            await session.DeleteAsync(media, GetResourceName(media)).ConfigureAwait(false);
                            var mediaId = await SaveUpdatMedia(content, media);
                            await _repo.Delete(media.Id).ConfigureAwait(false);

                            // added to handle add media for web
                            mediaIds.Add(media);

                        }
                    }
                }

                return mediaIds;
            }
            catch (SqlException ex)
            {

                throw ex;
            }

        }

        public async Task<MediaVM> SaveUpdatMedia(MediaContent content, EskaCMS.Entities.Entity.Core.Media oldMedia = null)
        {

            try
            {
                var type = MediaManager.GetItem(content.Filename);
                MediaVM model = new MediaVM();
                model.FolderId = content.FolderId;
                model.Type = MediaManager.GetMediaType(content.Filename);
                model.ContentType = type.ContentType;
                model.SiteId = content.SiteId;
                model.Order = content.Order;
                model.CultureId = content.CultureId;
                model.Force = content.Force;
                model.Status = content.Status != null ? content.Status.Value : EStatus.Published;

                string fileName = Path.GetFileNameWithoutExtension(content.Filename);

                var DuplicatedMedias = _mediaRepository.Query()
                                                     .Where(x => x.Filename.Contains(fileName) && x.SiteId == content.SiteId && x.FolderId == model.FolderId
                                                                     && x.ContentType.Contains(model.ContentType) &&
                                                                     (x.Status == EStatus.Published ||
                                                                     x.Status == EStatus.Unpublished))
                                                     .ToList();

                if (DuplicatedMedias.Any())
                {
                    content.Filename = $"{Path.GetFileNameWithoutExtension(content.Filename)}_{DuplicatedMedias.Count()}{Path.GetExtension(content.Filename)}";
                }


                model.Filename = content.Filename;
                model.PublicUrl = GetPublicUrl(model);



                // Pre-process if this is an image
                if (_processor != null && type.AllowProcessing && model.Type == MediaType.Image)
                {
                    byte[] bytes;

                    if (content is BinaryMediaContent)
                    {
                        bytes = ((BinaryMediaContent)content).Data;
                    }
                    else
                    {
                        var reader = new BinaryReader(((StreamMediaContent)content).Data);
                        bytes = reader.ReadBytes((int)reader.BaseStream.Length);
                        ((StreamMediaContent)content).Data.Position = 0;
                    }

                    int width, height;

                    _processor.GetSize(bytes, out width, out height);
                    model.Width = width;
                    model.Height = height;
                }

                // Upload to storage
                using (var session = await _storage.OpenAsync().ConfigureAwait(false))
                {
                    if (content is BinaryMediaContent)
                    {
                        var bc = (BinaryMediaContent)content;

                        model.Size = bc.Data.Length;
                        await session.PutAsync(model, model.Filename,
                            model.ContentType, bc.Data).ConfigureAwait(false);
                    }
                    else if (content is StreamMediaContent)
                    {
                        //if (model.Force != false)
                        //{



                        //if (DuplicatedMedia != null)
                        //{
                        //    RemoveFile(DuplicatedMedia.PublicUrl);

                        //}

                        //}



                        var sc = (StreamMediaContent)content;
                        var stream = sc.Data;



                        model.Size = sc.Data.Length;
                        await session.PutAsync(model, model.Filename,
                            model.ContentType, stream).ConfigureAwait(false);


                    }
                }

                if (model.Type == MediaType.Image)
                {
                    if (!model.ContentType.Contains("image/webp") && !model.ContentType.Contains("image/svg"))
                    {
                        Bitmap image = new Bitmap(((StreamMediaContent)content).Data);
                        model.Width = image.Width;
                        model.Height = image.Height;
                    }



                }

                //if (oldMedia != null)
                //{
                //    model.CultureId = oldMedia.CultureId;
                //}

                return await _repo.Save(model).ConfigureAwait(false);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<List<EskaCMS.Entities.Entity.Core.Media>> GetByFileNameAsync(string filename, long siteId)
        {
            //  var model = _cache?.Get<Core.Entities.Media>(id.ToString());

            // if (model == null)
            {
                var model = await _repo.GetByFileName(filename, siteId).ConfigureAwait(false);

                OnLoad(model);
                //  }
                return model;
            }
        }

        /// <summary>
        /// Processes the model on load.
        /// </summary>
        /// <param name="model">The model</param>
        private void OnLoad(List<EskaCMS.Entities.Entity.Core.Media> model)
        {
            if (model != null)
            {
                foreach (var item in model)
                {
                    // Get public url
                    item.PublicUrl = GetPublicUrl(item);

                    // Create missing properties
                    //foreach (var key in MediaManager.MetaProperties)
                    //{
                    //    if (!item.Properties.Any(p => p.Key == key))
                    //    {
                    //        item.Properties.Add(key, null);
                    //    }
                    //}
                }
            }
        }

        private void RemoveFile(string Path)
        {
            File.Delete("wwwroot/" + Path);
        }

        public async Task<List<MediaDetailsWithOrderVM>> GetMediaDetailsAsync(long MediaId, long CultureId)
        {
            //  var model = _cache?.Get<Core.Entities.Media>(id.ToString());

            // if (model == null)
            {
                var model = await _repo.GetMediaDetails(MediaId, CultureId).ConfigureAwait(false);

                //  }
                return model;
            }
        }

        public async Task SaveMediaDetailsAsync(MediaDetailsWithOrderVM model)
        {
            await _repo.SaveMediaDetails(model).ConfigureAwait(false);

        }
        public async Task<List<MediaFolderVM>> GetByFolderNameAsync(string folderCode, long siteId)
        {
            var model = await _repo.GetByFolderName(folderCode, siteId).ConfigureAwait(false);
            return model;
        }

        public async Task<MediaFolderDetailsWithOrderVM> GetFoldersDetailsByCultureId(long FolderId, long CultureId, long SiteId)
        {
            var Folder = await _repo.GetFolderById(SiteId, FolderId, CultureId);
            if (Folder != null)
            {
                var Details = Folder.MediaFoldersDetails
                                .FirstOrDefault(x => x.CultureId == CultureId);
                if (Details == null)
                    return null;

                MediaFolderDetailsWithOrderVM Data = new MediaFolderDetailsWithOrderVM()
                {
                    Title = Details.Title,
                    Id = Details.Id,
                    Order = Folder.Order,
                    CreatedById = Details.CreatedById,
                    CreationDate = Details.CreationDate,
                    Culture = Details.Culture,
                    CultureId = Details.CultureId,
                    Description = Details.Description,
                    FolderId = FolderId,
                    ThumbnailId = Details.ThumbnailId,
                    ThumbnailMedia = Details.Thumbnail,
                    ModificationDate = Details.ModificationDate,
                    ModifiedById = Details.ModifiedById
                };

                return Data;

            }
            return null;
        }

        public async Task<bool> DeleteMediaWithDetails(long MediaId)
        {

            return await _repo.DeleteMediaWithDetails(new List<long> { MediaId });
        }

        public async Task MoveAsync(MediaVM model, long? folderId, string? OldBreadCramb, string? NewBreadCramb, bool Force)
        {
            await _repo.Move(model, folderId, OldBreadCramb, NewBreadCramb, Force).ConfigureAwait(false);

        }

        /// <summary>
        /// Adds or updates the given model in the database
        /// depending on its state.
        /// </summary>
        /// <param name="model">The model</param>
        public async Task SaveFolderAsync(MediaFolderVM model)
        {

            // Validate model
            var context = new ValidationContext(model);
            Validator.ValidateObject(model, context, true);
            await _repo.SaveFolder(model).ConfigureAwait(false);

        }

        public async Task<bool> setThumbnailFolder(MediaFolderVM media)
        {
            var updateThmbnail = await _repo.updateThmbnail(media);
            return updateThmbnail;

        }

        public async Task AssignFolderPermissions(RequestFolderRoleViewModel model)
        {
            try
            {
                if (model.FolderIds != null && model.FolderIds.Count() > 0)
                {
                    var oldfolderRoleToBeDeleted = await folderRoleRepository.Query().Where(x => model.FolderIds.Any(y => y == x.FolderId)).ToListAsync();
                    if (oldfolderRoleToBeDeleted != null && oldfolderRoleToBeDeleted.Count() > 0)
                    {

                        folderRoleRepository.RemoveRange(oldfolderRoleToBeDeleted);
                        await folderRoleRepository.SaveChangesAsync();

                    }

                    if (model.RoleIds != null && model.RoleIds.Count() > 0)
                    {
                        var UserId = await _WorkContext.GetCurrentUserId();
                        var AddFolderRole = new List<FolderRole>();
                        foreach (var FolderId in model.FolderIds)
                        {

                            AddFolderRole.AddRange(model.RoleIds.ConvertAll(x => new FolderRole
                            {

                                RoleId = x,
                                CreationDate = DateTime.Now,
                                CreatedById = UserId,
                                FolderId = FolderId,


                            }));


                        }
                        folderRoleRepository.AddRange(AddFolderRole);
                        await folderRoleRepository.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task AssignMediaPermissions(RequestMediaRoleViewModel model)
        {
            try
            {
                var UserId = await _WorkContext.GetCurrentUserId();

                if (model.AddMediaRole != null)
                {
                    var AddMediaRoles = new List<MediaRole>();
                    foreach (var roleId in model.AddMediaRole.RoleIds)
                    {


                        var AddMediaRole = model.AddMediaRole.MediaIds.ConvertAll(x => new MediaRole
                        {
                            RoleId = roleId,
                            CreationDate = DateTime.Now,
                            CreatedById = UserId,
                            MediaId = x,
                        });

                        AddMediaRoles.AddRange(AddMediaRole);
                    }

                    mediaRoleRepository.AddRange(AddMediaRoles);
                    await mediaRoleRepository.SaveChangesAsync();
                }
                else
                {
                    var oldmediaRoleToBeDeleted = await mediaRoleRepository.Query().Where(x => x.MediaId == model.MediaId).ToListAsync();
                    if (oldmediaRoleToBeDeleted != null && oldmediaRoleToBeDeleted.Count() > 0)
                    {

                        mediaRoleRepository.RemoveRange(oldmediaRoleToBeDeleted);
                        await mediaRoleRepository.SaveChangesAsync();

                    }
                    var AddMediaRole = model.RoleIds.ConvertAll(x => new MediaRole
                    {
                        RoleId = x,
                        CreationDate = DateTime.Now,
                        CreatedById = UserId,
                        MediaId = model.MediaId.Value,
                    });

                    mediaRoleRepository.AddRange(AddMediaRole);
                    await mediaRoleRepository.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<MediaAssignedRolesVM> AssignedMediaRoles(long Id)
        {
            try
            {

                var siteId = _WorkContext.GetCurrentSiteId();
                var MediaAssignedRoles = await mediaRoleRepository.Query()
                                                         .Include(x => x.Media)
                                                         .Where(x => x.MediaId == Id && x.Media.SiteId == siteId)
                                                         .Include(x => x.Role)
                                                         .Select(x => new Role
                                                         {
                                                             Id = x.Role.Id,
                                                             Name = x.Role.Name,
                                                             Type = x.Role.Type,
                                                         })
                                                         .ToListAsync();

                var assignedRoleIds = MediaAssignedRoles.Select(x => x.Id).ToList();

                var unassingedRoles = await roleManager.Roles.Where(x => (x.SiteId == siteId || x.SiteId == null) && !assignedRoleIds.Contains(x.Id)).Select(x =>
                      new Role
                      {
                          Id = x.Id,
                          Name = x.Name,
                          Type = x.Type,
                      }).ToListAsync();

                var result = new MediaAssignedRolesVM { AssingedRoles = MediaAssignedRoles, UnassingedRoles = unassingedRoles };

                return result;




            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<MediaAssignedRolesVM> AssignedFolderRoles(long Id)
        {
            try
            {
                var siteId = _WorkContext.GetCurrentSiteId();
                var FolderAssignedRoles = await folderRoleRepository.Query()
                                                         .Include(x => x.Folder)
                                                         .Where(x => x.FolderId == Id && x.Folder.SiteId == siteId)
                                                         .Include(x => x.Role)
                                                         .Select(x => new Role
                                                         {
                                                             Id = x.Role.Id,
                                                             Name = x.Role.Name,
                                                             Type = x.Role.Type,
                                                         })
                                                         .ToListAsync();

                var assignedRoleIds = FolderAssignedRoles.Select(x => x.Id).ToList();

                var unassingedRoles = await roleManager.Roles.Where(x => (x.SiteId == siteId || x.SiteId == null) && !assignedRoleIds.Contains(x.Id)).Select(x =>
                      new Role
                      {
                          Id = x.Id,
                          Name = x.Name,
                          Type = x.Type,
                      }).ToListAsync();

                var result = new MediaAssignedRolesVM { AssingedRoles = FolderAssignedRoles, UnassingedRoles = unassingedRoles };

                return result;


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #region UploadPhysicalMediaShared
        public async Task<string> UploadSystemMedia(IFormFile file)
        {
            try
            {


                if (file == null || file.Length == 0)
                    throw new ArgumentException("The file is invalid or empty.", nameof(file));

                string folderPath = Path.Combine(_basePath, SystemAssetsFolderName);
                EnsureDirectoryExists(folderPath);

                string filePath = Path.Combine(folderPath, file.FileName);

                if (File.Exists(filePath))
                    throw new IOException($"A file with the name '{file.FileName}' already exists.");

                await SaveFileAsync(file, filePath);

                return $"/Resources/{SystemAssetsFolderName}/{file.FileName}";

            }
            catch (Exception)
            {

                throw;
            }
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private async Task SaveFileAsync(IFormFile file, string path)
        {
            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);
        }

        public async Task<int> ScanAndSaveFoldersAsync(string rootFolderPath)
        {
            string fullPath = Path.Combine(rootFolderPath);

            if (!Directory.Exists(fullPath))
            {
                throw new DirectoryNotFoundException($"Directory '{rootFolderPath}' not found!");
            }

            try
            {
                long rootFolderId = await SaveFolderAsync(new DirectoryInfo(fullPath).Name, null, 1);
                return await ScanFolderRecursively(fullPath, rootFolderId, 1);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<int> ScanFolderRecursively(string currentPath, long parentFolderId, long siteId)
        {
            int savedImageCount = 0;
            string folderName = new DirectoryInfo(currentPath).Name;

            try
            {
                long folderId = await SaveFolderAsync(folderName, parentFolderId, siteId);

                foreach (var file in Directory.EnumerateFiles(currentPath, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => IsSupportedImageFile(f)))
                {
                    try
                    {
                        await ProcessImageFileAsync(file, folderId, siteId);
                        savedImageCount++;
                    }
                    catch (Exception ex)
                    {
                    }
                }

                foreach (var subFolder in Directory.EnumerateDirectories(currentPath))
                {
                    savedImageCount += await ScanFolderRecursively(subFolder, folderId, siteId);
                }
            }
            catch (Exception ex)
            {
            }

            return savedImageCount;
        }

        private bool IsSupportedImageFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension switch
            {
                ".png" => true,
                ".jpg" => true,
                ".jpeg" => true,
                ".gif" => true,
                ".bmp" => true,
                ".webp" => true,
                _ => false
            };
        }

        private async Task ProcessImageFileAsync(string filePath, long folderId, long siteId)
        {
            var fileInfo = new FileInfo(filePath);
            string relativePath = filePath.Replace("\\", "/");

   

            try
            {

                var imageFile = new EskaCMS.Entities.Entity.Core.Media
                {
                    FolderId = folderId,
                    Filename = fileInfo.Name,
                    CreatedById=1,
                    Status=EStatus.Active,

                    ContentType = GetContentType(fileInfo.Extension),
                    Size = fileInfo.Length,
                    PublicUrl = relativePath,
                    Width = 1000,
                    Height = 1000,
                    CreationDate = fileInfo.CreationTimeUtc,
                    SiteId = siteId
                };

                _mediaRepository.Add(imageFile);
                await _mediaRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

      

        private async Task<long> SaveFolderAsync(string folderName, long? parentFolderId, long siteId)
        {
            var existingFolder = await _mediafoldersRepository.Query()
                .Include(x=>x.MediaFoldersDetails)
                .FirstOrDefaultAsync(f => f.MediaFoldersDetails.Any(c=>c.Title == folderName) &&
                                        f.ParentId == parentFolderId &&
                                        f.SiteId == siteId);

            if (existingFolder != null)
                return existingFolder.Id;

            var folder = new MediaFolders
            {
                ParentId = parentFolderId,
                SiteId = siteId,
                CreationDate=DateTime.UtcNow,
                Order = 0,
                
            };

            _mediafoldersRepository.Add(folder);
            await _mediafoldersRepository.SaveChangesAsync();
            var FolderDetails = new MediaFoldersDetails
            {
                FolderId = folder.Id,
                CreatedById = 1,
                Description = folderName,
                CultureId = 1,
                Title = folderName
               ,CreationDate = DateTime.UtcNow, 

            };
            _mediafoldersdetailsRepository.Add(FolderDetails);
            await _mediafoldersdetailsRepository.SaveChangesAsync();
            return folder.Id;
        }

        private string GetContentType(string extension)
        {
            return extension.ToLower() switch
            {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }


        #endregion
    }
}
