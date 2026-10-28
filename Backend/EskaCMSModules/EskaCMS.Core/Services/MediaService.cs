using EskaCMS.Entities.Entity;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using System.Text;
using EskaCMS.Core.Repositories;
using EskaCMS.Module.Core;
using System.ComponentModel.DataAnnotations;
using System;

using EskaCMS.Core.Runtime;
using EskaCMS.Core.Models;
using System.Drawing;
using Microsoft.Data.SqlClient;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Core.ViewModels;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Enum;

namespace EskaCMS.Core.Services
{
    public class MediaService : IMediaService
    {
        private readonly IMediaRepository _repo;
        //  private readonly IParamService _paramService;
        private readonly IStorage _storage;
        private readonly IImageProcessor _processor;
        // private readonly ICache _cache;
        // Async-capable mutual exclusion so the critical section below can AWAIT the save
        // instead of blocking a thread (.Wait()). Static SemaphoreSlim(1,1) preserves the
        // exact same global one-at-a-time serialization the previous `lock` provided.
        private static readonly System.Threading.SemaphoreSlim ScaleMutex = new System.Threading.SemaphoreSlim(1, 1);
        private const string MEDIA_STRUCTURE = "MediaStructure";
        private readonly IRepository<MediaFolders> _mediafoldersRepository;
        private readonly IRepository<Media> _mediaRepository;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="repo">The current repository</param>
        /// <param name="paramService">The current param service</param>
        /// <param name="storage">The current storage manager</param>
        /// <param name="cache">The optional model cache</param>
        /// <param name="processor">The optional image processor</param>
        public MediaService(IMediaRepository repo,IRepository<MediaFolders> mediafoldersRepository, IStorage storage, IImageProcessor processor = null, IRepository<Media> mediaRepository=null)
        {
            _repo = repo;
            //  _paramService = paramService;
            _storage = storage;
            _processor = processor;
            MediaManager.Init();
            //  _cache = cache;
            _mediafoldersRepository = mediafoldersRepository;
            _mediaRepository=mediaRepository;
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
         

        
        public async Task<MediaFolderDetailsWithOrderVM> GetFoldersDetailsByCultureId(long FolderId , long CultureId , long SiteId)
        {
            var Folder = await _repo.GetFolderById(SiteId, FolderId, CultureId);
            if (Folder != null)
            {
                var Details = Folder.MediaFoldersDetails
                                .FirstOrDefault(x=>x.CultureId == CultureId );
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
        /// <summary>
        /// Gets all media available in the specified folder.
        /// </summary>
        /// <param name="folderId">The optional folder id</param>
        /// <returns>The available media</returns>
        public Task<IEnumerable<MediaVM>> GetAllByFolderIdAsync(long siteId, long? folderId = null,string SearchText=null)
        {
            try
            {
                return _repo.GetAll(siteId, folderId, SearchText).ContinueWith(t => _getFast(t.Result.ToArray())).Unwrap();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        /// <inheritdoc cref="IMediaService.CountFolderItemsAsync"/>
        public Task<int> CountFolderItemsAsync(long? folderId = null) => _repo.CountAll(folderId);

        /// <summary>
        /// Gets all media folders available in the specified
        /// folder.
        /// </summary>
        /// <param name="folderId">The optional folder id</param>
        /// <returns>The available media folders</returns>
        public async Task<IEnumerable<MediaFolderVM>> GetAllFoldersAsync(long siteId, long? folderId = null)
        {
            var models = new List<MediaFolderVM>();
            var items = await _repo.GetAllFolders(siteId, folderId).ConfigureAwait(false);

            foreach (var item in items)
            {
                var folder = await GetFolderByIdAsync(siteId, item).ConfigureAwait(false);

                if (folder != null)
                {
                    models.Add(folder);
                }
            }
            return models;
        }

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

        /// <summary>
        /// Get all media matching the given IDs.
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>

        public Task<IEnumerable<MediaVM>> GetByIdAsync(params long[] ids)
        {
            return _repo.GetById(ids);
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
        /// Gets the hierachical media structure.
        /// </summary>
        /// <returns>The media structure</returns>
        public async Task<MediaStructure> GetStructureAsync(long siteId, long CultureId = 0 )
        {
           
            var structure = await _repo.GetStructure(siteId,null, CultureId).ConfigureAwait(false);


            return structure;
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

                List<Media> mediaList = new List<Media>();
                var oldMedia = new Media();
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

        //public async Task UpdateAsync(MediaVM model)
        //{
        //    // Make sure we have an existing media model with this id.
        //    var current = await GetByIdAsync(model.Id);

        //    if (current != null)
        //    {
        //        // Validate model
        //        var context = new ValidationContext(model);
        //        Validator.ValidateObject(model, context, true);


        //        await _repo.Save(model).ConfigureAwait(false);

        //    }
        //    else
        //    {
        //        throw new FileNotFoundException("You can only update meta data for an existing media object");
        //    }
        //}



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

        /// <summary>
        /// Moves the media to the folder with the specified id.
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="folderId">The folder id</param>
        public async Task MoveAsync(MediaVM model, long? folderId, string? OldBreadCramb, string? NewBreadCramb,bool Force)
        { 
            await _repo.Move(model, folderId, OldBreadCramb, NewBreadCramb,Force).ConfigureAwait(false);

        }

        /// <summary>
        /// Ensures that the image version with the given size exsists
        /// and returns its public URL.
        /// </summary>
        /// <param name="id">The unique id</param>
        /// <param name="width">The requested width</param>
        /// <param name="height">The optionally requested height</param>
        /// <returns>The public URL</returns>
        public string EnsureVersion(long id, int width, int? height = null)
        {
            return EnsureVersionAsync(id, width, height).GetAwaiter().GetResult();
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

                        await ScaleMutex.WaitAsync();
                        try
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

                                await _repo.Save(media);


                                upload = true;
                            }
                        }
                        finally
                        {
                            ScaleMutex.Release();
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

        /// <summary>
        /// Deletes the media with the given id.
        /// </summary>
        /// <param name="id">The unique id</param>
        public async Task DeleteAsync(long id)
        {
            var media = await GetByIdAsync(id).ConfigureAwait(false);

            if (media != null)
            {
                using (var session = await _storage.OpenAsync().ConfigureAwait(false))
                {
                    // Delete all versions
                    if (media.Versions.Count > 0)
                    {
                        foreach (var version in media.Versions)
                        {
                            // Delete version from storage
                            await session.DeleteAsync(media, GetResourceName(media, version.Width, version.Height, version.FileExtension))
                                .ConfigureAwait(false);
                        }
                    }

                    await _repo.Delete(id).ConfigureAwait(false);
                    await session.DeleteAsync(media, media.Filename).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Deletes the given model.
        /// </summary>
        /// <param name="model">The media</param>
        public Task DeleteAsync(MediaVM model)
        {
            try
            {
                return DeleteAsync(model.Id);
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }


        public async Task<bool> DeleteMediaWithDetails(long MediaId)
        {
           return await _repo.DeleteMediaWithDetails(new List<long> { MediaId });
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
        /// Deletes the given model.
        /// </summary>
        /// <param name="model">The media</param>
        public Task DeleteFolderAsync(MediaFolderVM model)
        {
            return DeleteFolderAsync(model.SiteId, model.Id);
        }

        /// <summary>
        /// Processes the model on load.
        /// </summary>
        /// <param name="model">The model</param>
        private void OnLoad(List<Media> model)
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

        private void OnLoad(MediaVM model)
        {
            if (model != null)
            {
                // Get public url
                model.PublicUrl = GetPublicUrl(model);

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
                    } else
                    {
                        sb.Append(filename.Name);
                    }
                }
                else
                {
                 if(!string.IsNullOrEmpty(filename.Extension))
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

        public async Task<List<Media>> GetByFileNameAsync(string filename, long siteId)
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


        public async Task<List<MediaDetailsWithOrderVM>> GetMediaDetailsAsync(long MediaId,long CultureId)
        {
            //  var model = _cache?.Get<Core.Entities.Media>(id.ToString());

            // if (model == null)
            {
                var model = await _repo.GetMediaDetails(MediaId , CultureId).ConfigureAwait(false);

                //  }
                return model;
            }
        }


        public async Task<List<MediaDetailsWithOrderVM>> GetMediaDetailsAsync(long MediaId)
        {
           
            {
                var model = await _repo.GetMediaDetails(MediaId).ConfigureAwait(false);

                return model;
            }
        }


        public async Task<List<MediaFolderVM>> GetByFolderNameAsync(string folderCode, long siteId)
        {
            var model = await _repo.GetByFolderName(folderCode, siteId).ConfigureAwait(false);
            return model;
        }
        public async Task<MediaVM> SaveUpdatMedia(MediaContent content, Media oldMedia = null)
        {
            var type = MediaManager.GetItem(content.Filename);
            MediaVM model = new MediaVM();
            model.Filename = content.Filename.Replace(" ", "_");
            model.FolderId = content.FolderId;
            model.Type = MediaManager.GetMediaType(content.Filename);
            model.ContentType = type.ContentType;
            model.SiteId = content.SiteId;
            model.Order = content.Order;
            model.CultureId = content.CultureId;
            model.PublicUrl = GetPublicUrl(model);
            model.Force = content.Force;
            


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
                    if(model.Force!=false)
                    {
                        var DuplicatedMedia=  _mediaRepository.Query().Where(x => x.Filename == model.Filename && x.SiteId == model.SiteId && x.FolderId == model.FolderId).FirstOrDefault();
                        if(DuplicatedMedia != null)
                        {
                            RemoveFile(DuplicatedMedia.PublicUrl);

                        }
                    }

                    var sc = (StreamMediaContent)content;
                    var stream = sc.Data;

                    model.Size = sc.Data.Length;
                    await session.PutAsync(model, model.Filename,
                        model.ContentType, stream).ConfigureAwait(false);
                }
            }
            if (model.Type == MediaType.Image)
            {
                Bitmap image = new Bitmap(((StreamMediaContent)content).Data);
                model.Width = image.Width;
                model.Height = image.Height;
            }
            //if (oldMedia != null)
            //{
            //    model.CultureId = oldMedia.CultureId;
            //}

            return await _repo.Save(model).ConfigureAwait(false);

        }
        private void RemoveFile(string Path)
        {
            File.Delete("wwwroot/" + Path);
        }
        public MediaFolders GetFolderById(long folderId)
        {
            var folder = _mediafoldersRepository.Query().Where(x => x.Id == folderId).FirstOrDefault();
            return folder;
        }
        public MediaFolders GetFolderWithDetails(long folderId)
        {
            return _repo.GetFolderWithDetails(folderId);
        }

        public async Task SaveMediaDetailsAsync(MediaDetailsWithOrderVM model)
        {
            await _repo.SaveMediaDetails(model).ConfigureAwait(false);
       
        }
        public async Task<bool> setThumbnailFolder(MediaFolderVM media)
        {
            var updateThmbnail = await _repo.updateThmbnail(media);
            return updateThmbnail;

        }

    }
}