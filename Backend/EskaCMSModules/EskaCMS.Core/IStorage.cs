

using EskaCMS.Core.Models;
using System.Threading.Tasks;

using EskaCMS.Entities.Entity;


namespace EskaCMS.Module.Core
{
    /// <summary>
    /// Interface for the main storage manager.
    /// </summary>
    public interface IStorage
    {
        /// <summary>
        /// Opens a new storage session.
        /// </summary>
        /// <returns>A new open session</returns>
        Task<IStorageSession> OpenAsync();

        /// <summary>
        /// Gets the public URL for the given media object.
        /// </summary>
        /// <param name="media">The media file</param>
        /// <param name="filename">The file name</param>
        /// <returns>The public url</returns>
        string GetPublicUrl(MediaVM media, string filename);

        /// <summary>
        /// Gets the resource name for the given media object.
        /// </summary>
        /// <param name="media">The media file</param>
        /// <param name="filename">The file name</param>
        /// <returns>The public url</returns>
        string GetResourceName(MediaVM media, string filename);
    }
}
