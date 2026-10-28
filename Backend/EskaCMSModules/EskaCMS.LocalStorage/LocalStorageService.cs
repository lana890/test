using System.IO;
using System.Threading.Tasks;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure;
using EskaCMS.Media.Models;
using EskaCMS.Module.Core;
using EskaCommerce.Infrastructure;

namespace EskaCMS.StorageLocal
{
    public class LocalStorageService : IStorageService
    {
        private const string MediaRootFoler = "Resources";
        private readonly string _basePath = "wwwroot/uploads/";
        private readonly string _baseUrl = "~/uploads/";
        private readonly FileStorageNaming _naming;
        public string GetMediaUrl(string fileName)
        {
            return $"{fileName}";
        }

        public async Task SaveMediaAsync(Stream mediaBinaryStream, string fileName, string mimeType = null)
        {
            var filePath = Path.Combine(GlobalConfiguration.WebRootPath, MediaRootFoler, fileName);
            using (var output = new FileStream(filePath, FileMode.Create))
            {
                await mediaBinaryStream.CopyToAsync(output);
            }
        }

        public async Task DeleteMediaAsync(string fileName)
        {
            var filePath = Path.Combine(GlobalConfiguration.WebRootPath, MediaRootFoler, fileName);
            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath));
            }
        }
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="basePath">The optional base path</param>
        /// <param name="baseUrl">The optional base url</param>
        /// <param name="naming">How uploaded media files should be named</param>
        public FileStorage(
            string basePath = null,
            string baseUrl = null,
            FileStorageNaming naming = FileStorageNaming.UniqueFileNames)
        {
            if (!string.IsNullOrEmpty(basePath))
            {
                _basePath = basePath;
            }
            if (!string.IsNullOrEmpty(baseUrl))
            {
                _baseUrl = baseUrl;
            }

            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }

            _naming = naming;
        }

        /// <summary>
        /// Opens a new storage session.
        /// </summary>
        /// <returns>A new open session</returns>
        public Task<IStorageSession> OpenAsync()
        {
            return Task.Run(() =>
            {
                return (IStorageSession)new FileStorageSession(this, _basePath, _baseUrl, _naming);
            });
        }

        /// <summary>
        /// Gets the public URL for the given media object.
        /// </summary>
        /// <param name="media">The media file</param>
        /// <param name="filename">The file name</param>
        /// <returns>The public url</returns>
        public string GetPublicUrl(MediaViewModel media, string filename)
        {
            if (media != null && !string.IsNullOrWhiteSpace(filename))
            {
                return _baseUrl + GetResourceName(media, filename, true);
            }
            return null;
        }

        /// <summary>
        /// Gets the resource name for the given media object.
        /// </summary>
        /// <param name="media">The media file</param>
        /// <param name="filename">The file name</param>
        /// <returns>The public url</returns>
        public string GetResourceName(MediaViewModel media, string filename)
        {
            return GetResourceName(media, filename, false);
        }

        /// <summary>
        /// Gets the resource name for the given media object.
        /// </summary>
        /// <param name="media">The media file</param>
        /// <param name="filename">The file name</param>
        /// <param name="encode">If the filename should be URL encoded</param>
        /// <returns>The public url</returns>
        public string GetResourceName(MediaViewModel media, string filename, bool encode)
        {
            if (media != null && !string.IsNullOrWhiteSpace(filename))
            {
                var path = "";

                if (_naming == FileStorageNaming.UniqueFileNames)
                {
                    path = $"{ media.Id }-{ (encode ? System.Web.HttpUtility.UrlPathEncode(filename) : filename) }";
                }
                else
                {
                    path = $"{ media.Id }/{ (encode ? System.Web.HttpUtility.UrlPathEncode(filename) : filename) }";
                }
                return path;
            }
            return null;
        }
    }

}

