

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Media.Models
{
    /// <summary>
    /// Model for uploading a new media asset.
    /// </summary>
    public class MediaUploadModel
    {
        /// <summary>
        /// Gets/sets the optional id.
        /// </summary>
        public long? Id { get; set; }

        /// <summary>
        /// Gets/sets the parent id.
        /// </summary>
        public long? ParentId { get; set; }

        public long CreatedBy { get; set; }
        public long SiteId { get; set; }
        public long CultureId { get; set; }
        public bool? Force { get; set; }
        public bool? UniqueName { get; set; }
        public bool? ConvertToWebp { get; set; }=false;
        public EStatus? status { get; set; }

        /// <summary>
        /// Gets/sets the uploaded file.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IFormFile> Uploads { get; set; } = new List<IFormFile>();
      
    }
}
