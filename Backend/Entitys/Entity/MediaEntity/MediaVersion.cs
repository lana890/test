

using EskaCMS.Infrastructure.Models;
using System;

namespace EskaCMS.Entities.Entity.Core
{
    [Serializable]
    public class MediaVersion :EntityBase
    {
    

        /// <summary>
        /// Gets/sets the file size in bytes.
        /// </summary>
        public long Size { get; set; }


        /// <summary>
        /// Gets/sets the width.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Gets/sets the optional height.
        /// </summary>
        public int? Height { get; set; }


        public long MediaId { get; set; }

        public virtual Media  Media { get; set; }
        /// <summary>
        /// Gets/sets the file extension of the generated version.
        /// </summary>
        public string FileExtension { get; set; }
    }
}