

using System;
using System.IO;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Models
{
    /// <summary>
    /// Abstract class for media content.
    /// </summary>
    public abstract class MediaContent
    {
        /// <summary>
        /// Gets/sets the optional id. If this is empty a new media
        /// entry is created, otherwise the specified media
        /// is updated.
        /// </summary>
        public long? Id { get; set; }

        /// <summary>
        /// Gets/sets the optional folder id this media should be placed in.
        /// </summary>
        public long? FolderId { get; set; }

        /// <summary>
        /// Gets/sets the filename.
        /// </summary>
        public string Filename { get; set; }
        public string PublicUrl { get; set; }
        public string FolderPath { get; set; }
        public long CreatedById { get; set; }
        public long SiteId { get; set; }
        public long CultureId { get; set; }
        public long? Order { get; set; }
        public bool? Force { get; set; }
        public EStatus? Status { get; set; }
    }

    /// <summary>
    /// Binary media content.
    /// </summary>
    public class BinaryMediaContent : MediaContent
    {
        /// <summary>
        /// Gets/sets the byte data of the media.
        /// </summary>
        public byte[] Data { get; set; }
    }

    /// <summary>
    /// Stream media content.
    /// </summary>
    public class StreamMediaContent : MediaContent
    {
        /// <summary>
        /// Gets/sets the stream the contains the media content.
        /// </summary>
       
        public Stream Data { get; set; }
    }
}
