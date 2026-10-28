

using EskaCMS.Entities.Entity;
using EskaCMS.Core.Models;
using EskaCMS.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Core.Services
{
    public interface IMediaService
    {
        /// <summary>
        /// Gets all media available in the specified folder.
        /// </summary>
        /// <param name="folderId">The optional folder id</param>
        /// <returns>The available media</returns>
        Task<IEnumerable<MediaVM>> GetAllByFolderIdAsync(long siteId, long? folderId = null, string SearchText = null);

        /// <summary>
        /// Get the amount of media available in the specified folder.
        /// </summary>
        /// <param name="folderId">The optional folder id</param>
        /// <returns>The quantity of media items present.</returns>
        Task<int> CountFolderItemsAsync(long? folderId = null);

        /// <summary>
        /// Gets all media folders available in the specified
        /// folder.
        /// </summary>
        /// <param name="folderId">The optional folder id</param>
        /// <returns>The available media folders</returns>
        Task<IEnumerable<MediaFolderVM>> GetAllFoldersAsync(long siteId, long? folderId = null);

        /// <summary>
        /// Gets the media with the given id.
        /// </summary>
        /// <param name="id">The unique id</param>
        /// <returns>The media</returns>
        Task<MediaVM> GetByIdAsync(long id);

        /// <summary>
        /// Gets the media with the given id.
        /// </summary>
        /// <param name="ids">The unique id</param>
        /// <returns>The media</returns>
        Task<IEnumerable<MediaVM>> GetByIdAsync(params long[] ids);

        /// <summary>
        /// Gets the media folder with the given id.
        /// </summary>
        /// <param name="id">The unique id</param>
        /// <returns>The media folder</returns>
        Task<MediaFolderVM> GetFolderByIdAsync(long siteId, long id);

        /// <summary>
        /// Gets the hierachical media structure.
        /// </summary>
        /// <returns>The media structure</returns>
        Task<MediaStructure> GetStructureAsync(long siteId, long CultureId =0 );

        /// <summary>
        /// Updates the meta data for the given media model.
        /// </summary>
        /// <param name="model">The model</param>
        Task SaveAsync(MediaVM model);

        /// <summary>
        /// Adds or updates the given model in the database
        /// depending on its state.
        /// </summary>
        /// <param name="content">The content to save</param>
        Task<List<MediaVM>> SaveAsync(MediaContent content);

        //Task UpdateAsync(MediaVM model);


        /// <summary>
        /// Adds or updates the given model in the database
        /// depending on its state.
        /// </summary>
        /// <param name="model">The model</param>
        Task SaveFolderAsync(MediaFolderVM model);

        /// <summary>
        /// Moves the media to the folder with the specified id.
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="folderId">The folder id</param>
        Task MoveAsync(MediaVM model, long? folderId, string? OldBreadCramb, string? NewBreadCramb,bool Force=false);

        /// <summary>
        /// Ensures that the image version with the given size exsists
        /// and returns its public URL.
        /// </summary>
        /// <param name="id">The unique id</param>
        /// <param name="width">The requested width</param>
        /// <param name="height">The optionally requested height</param>
        /// <returns>The public URL</returns>
        string EnsureVersion(long id, int width, int? height = null);

        /// <summary>
        /// Ensures that the image version with the given size exsists
        /// and returns its public URL.
        /// </summary>
        /// <param name="id">The unique id</param>
        /// <param name="width">The requested width</param>
        /// <param name="height">The optionally requested height</param>
        /// <returns>The public URL</returns>
        Task<string> EnsureVersionAsync(long id, int width, int? height = null);

        Task<string> EnsureVersionAsync(MediaVM media, int width, int? height = null);

        /// <summary>
        /// Deletes the media with the given id.
        /// </summary>
        /// <param name="id">The unique id</param>
        Task DeleteAsync(long id);

        /// <summary>
        /// Deletes the given model.
        /// </summary>
        /// <param name="model">The media</param>
        Task DeleteAsync(MediaVM model);

        /// <summary>
        /// Deletes the media folder with the given id.
        /// </summary>
        /// <param name="id">The unique id</param>
        Task DeleteFolderAsync(long siteId, long id);

        /// <summary>
        /// Deletes the given model.
        /// </summary>
        /// <param name="model">The media</param>
        Task DeleteFolderAsync(MediaFolderVM model);

        /// <summary>
        /// get media by filename and cultre Id 
        /// </summary>
        /// <param name="model">The media</param>
        public Task<List<Media>> GetByFileNameAsync(string filename, long siteId);

        public Task<List<MediaFolderVM>> GetByFolderNameAsync(string folderCode, long siteId);
        public MediaFolders GetFolderById(long folderId);
        public MediaFolders GetFolderWithDetails(long folderId);

        Task<List<MediaDetailsWithOrderVM>> GetMediaDetailsAsync(long MediaId);
        Task<List<MediaDetailsWithOrderVM>> GetMediaDetailsAsync(long MediaId, long CultureId );
        Task SaveMediaDetailsAsync(MediaDetailsWithOrderVM model);
        Task<bool> DeleteMediaWithDetails(long MediaId);

        Task<MediaFolderDetailsWithOrderVM> GetFoldersDetailsByCultureId(long FolderId, long CultureId, long SiteId);
        Task<bool> setThumbnailFolder(MediaFolderVM media);

    }
}