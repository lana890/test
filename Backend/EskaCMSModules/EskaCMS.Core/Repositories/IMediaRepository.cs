
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Models;
using EskaCMS.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Repositories
{
    public interface IMediaRepository
    {
        /// <summary>
        /// Gets all media available in the specified folder.
        /// </summary>
        /// <param name="folderId">The optional folder id</param>
        /// <returns>The available media</returns>
        Task<IEnumerable<long>> GetAll(long siteId, long? folderId = null,string SearchText=null, EStatus? Status = EStatus.Published, List<string> ContentType = null);

        /// <summary>
        /// Count the amount of items in the given folder.
        /// </summary>
        /// <param name="folderId">The optional folder id</param>
        /// <returns></returns>
        Task<int> CountAll(long? folderId);

        /// <summary>
        /// Gets all media folders available in the specified
        /// folder.
        /// </summary>
        /// <param name="folderId">The optional folder id</param>
        /// <returns>The available media folders</returns>
        Task<IEnumerable<long>> GetAllFolders(long siteId, long? folderId = null);

        /// <summary>
        /// Get media for all Ids in this enumerable.
        /// </summary>
        /// <param name="ids">One or several media id</param>
        /// <returns>The matching media</returns>
        Task<IEnumerable<MediaVM>> GetById(params long[] ids);

        /// <summary>
        /// Gets the media with the given id.
        /// </summary>
        /// <param name="id">The unique id</param>
        /// <returns>The media</returns>
        Task<MediaVM> GetById(long id);

        /// <summary>
        /// Gets the media folder with the given id.
        /// </summary>
        /// <param name="id">The unique id</param>
        /// <returns>The media folder</returns>
        Task<MediaFolderVM> GetFolderById(long siteId, long id, long CultureId=0);

        /// <summary>
        /// Gets the hierachical media structure.
        /// </summary>
        /// <returns>The media structure</returns>
        Task<MediaStructure> GetStructure(long siteId,string search, long CultureId = 0, EStatus? Status = EStatus.Published, EMediaFilter? orderFilter = EMediaFilter.NewToOld);

        /// <summary>
        /// Adds or updates the given model in the database.
        /// </summary>
        /// <param name="model">The model</param>
        Task<MediaVM> Save(MediaVM model);

        /// <summary>
        /// Adds or updates the given model in the database
        /// depending on its state.
        /// </summary>
        /// <param name="model">The model</param>
        Task SaveFolder(MediaFolderVM model);

        /// <summary>
        /// Moves the media to the folder with the specified id.
        /// </summary>
        /// <param name="model">The media</param>
        /// <param name="folderId">The folder id</param>
        Task Move(MediaVM model, long? folderId,string? OldBreadCramb, string? NewBreadCramb, bool Force=false);

        /// <summary>
        /// Deletes the media with the given id. Please note that this method
        /// is not really synchronous, it's just a wrapper for the async version.
        /// </summary>
        /// <param name="id">The unique id</param>
        Task Delete(long id);

        /// <summary>
        /// Deletes the media folder with the given id.
        /// </summary>
        /// <param name="id">The unique id</param>
        Task DeleteFolder(long id);

        Task<List<Media>> GetByFileName(string filename, long siteId);
        Task<List<MediaDetailsWithOrderVM>> GetMediaDetails(long MediaId, long CultureId = 0);
        Task<List<MediaFolderVM>> GetByFolderName(string folderCode, long siteId);
        MediaFolders GetFolderWithDetails(long folderId);
        Task SaveMediaDetails(MediaDetailsWithOrderVM model);
        Task<bool> DeleteMediaWithDetails(List<long> MediaIds);
        Task<bool> updateThmbnail(MediaFolderVM mediaFolder);

        Task<IEnumerable<long>> GetAllByMultibleFolders(long siteId, List<long> folderIds);

        Task<List<long>> GetAllChildrenFolders(long siteId, long folderId, List<MediaFolders> allFolders);
        Task<bool> SoftDeleteFolder(List<long> foldersIds, long siteId);
        Task<bool> SoftDeleteMedia(List<long> MediaIds);
        Task<bool> RetrieveMedia(long MediaId);
        Task RetrieveFolder(long id);
        Task<List<MediaFolders>> GetAllFolders();



    }
}