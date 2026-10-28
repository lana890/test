using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Models;
using EskaCMS.Core.ViewModels;
using EskaCMS.Media.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Media.Enums;
using Microsoft.AspNetCore.Http;

namespace EskaCMS.Media.Services.Interfaces
{
    public interface IMediaService
    {

        public Task<MediaListModel.MediaItem> GetById(long id);

        Task<List<MediaFolderSimple>> GetFolderBreadCrumb(long? FolderId, long? ParentFolderId, EStatus? status = EStatus.Published);
        /// <summary>
        /// Gets the breadcrumb list of the folders from the selected folder id
        /// </summary>
        /// <param name="structure">The complete media folder structure</param>
        /// <param name="folderId">The folder id</param>
        /// <returns></returns>
        public Task<List<MediaFolderSimple>> GetFolderBreadCrumb(MediaStructure structure, long? folderId);

        public Task<MediaListModel> GetList(long siteId, long? folderId = null, List<MediaType>? filter = null,
            int? width = null, int? height = null,int ?PageSizefolder=null,
            int? PageNumberfolder=null, int? pageSizemedia = null, int? pageNumbermedia = null,string SearchText=null, EMediaFilter? orderFilter = null, EStatus? Status = EStatus.Published, List<string> ContentType = null);



        public Task SaveFolder(RequestMediaFolderVM model);



        public Task<long?> DeleteFolder(long siteId, long id);



        public Task<List<MediaVM>> SaveMedia(MediaUploadModel model);

        public Task<MediaDetailsWithOrderVM> GetMeta(long mediaId, long siteId, long cultureId);
        public Task<MediaFolderVM> GetFolderByCulture(string folderCode, long siteId, long cultureId);

        public Task<bool> SaveMeta(MediaListModel.MediaItem media);

        public Task<long?> DeleteMedia(long id);
        Task<MediaInfo> GetMediaPysicalFile(string URL);
        Task<MediaFolderDetailsWithOrderVM> GetFolderDetailsByCultureId(long CultureId, long FolderId, long siteId);
        Task<FileExtension> GetFileExtensionFromBase64(CreateFileVM model);

        Task<MediaFolderVM> GetFolderByIdAsync(long siteId, long id);

        Task SaveFolderAsync(MediaFolderVM model);

        Task<MediaVM> GetByIdAsync(long id);

        Task<string> EnsureVersionAsync(long id, int width, int? height = null);
        Task<bool> setThumbnailFolder(MediaFolderVM media);

        Task MoveAsync(MediaVM model, long? folderId, string? OldBreadCramb, string? NewBreadCramb, bool Force);

        Task<List<long>> GetChildFoldersId(long siteId, long id);
        Task<long?> DeleteFoldersWithMedia(List<long> folderIds);
        Task<bool> DeleteMultibleMedia(List<long> mediaIds);

        Task<IEnumerable<MediaVM>> GetAllByFolderIdAsync(long siteId, long? folderId = null, string SearchText = null, EStatus? Status = EStatus.Published, List<string> ContentType = null);
        Task<MediaStructure> GetStructureAsync(long siteId,string search = null, long CultureId = 0, EStatus? Status = EStatus.Published, EMediaFilter? orderFilter = null);
        Task<bool> RetrieveFolderWithMedia(RestoreFolders folders);
        Task<bool> RetrieveMultibleMedia(RestoreMediaVM model);
        Task<bool> MoveFolders(TransferFolderVm model);
        Task<long?> SoftFoldersWithMedia(List<long> folderIds);
        Task<bool> SoftDeleteMedia(List<long> mediaIds);
        Task AssignFolderPermissions(RequestFolderRoleViewModel model);
        Task AssignMediaPermissions(RequestMediaRoleViewModel model);

        Task<MediaAssignedRolesVM> AssignedMediaRoles(long Id);
        Task<MediaAssignedRolesVM> AssignedFolderRoles(long Id);
        Task<string> UploadSystemMedia(IFormFile file);
        Task<int> ScanAndSaveFoldersAsync(string path);
    }
}
