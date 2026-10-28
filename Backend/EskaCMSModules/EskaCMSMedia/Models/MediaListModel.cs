

using EskaCMS.Core;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace EskaCMS.Media.Models
{
    /// <summary>
    /// List model for the media view.
    /// </summary>
    /// 

  



    public class MediaListModel
    {
        public static string ListView { get; } = "list";
        public static string GalleryView { get; } = "gallery";

        /// <summary>
        /// A folder item in the list view.
        /// </summary>
        public class FolderItem
        {
            /// <summary>
            /// Gets/sets the unique id.
            /// </summary>
            public long Id { get; set; }

            /// <summary>
            /// Gets/sets the name.
            /// </summary>
            public string Name { get; set; }
            public string Code { get; set; }

            /// <summary>
            /// Gets/sets the item count in the folder.
            /// </summary>
            public int ItemCount { get; set; }
            public int FolderCount { get; set; }
            public string ThumbnailUrl { get; set; }
            public long ThumbnailId { get;set; }
            public long? Order { get;set; }
            public EStatus Status { get; set; }


        }

        /// <summary>
        /// A media item in the list view.
        /// </summary>
        public class MediaItem
        {
            /// <summary>
            /// Gets/sets the unique id.
            /// </summary>
            public long Id { get; set; }

            /// <summary>
            /// Gets/sets the optional folder id.
            /// </summary>
            public long? FolderId { get; set; }

            /// <summary>
            /// Gets/sets the media type.
            /// </summary>
            public string Type { get; set; }

            /// <summary>
            /// Gets/sets the filename of the uploaded media.
            /// </summary>
            public string Filename { get; set; }

            /// <summary>
            /// Gets/sets the content type of the uploaded media.
            /// </summary>
            public string ContentType { get; set; }

            /// <summary>
            /// Gets/sets the optional title.
            /// </summary>
            public string Title { get; set; }

            /// <summary>
            /// Gets/sets the optional alt text.
            /// </summary>
            public string AltText { get; set; }

            /// <summary>
            /// Gets/sets the optional description.
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// Gets/sets the optional properties.
            /// </summary>
            public IList<KeyValuePair<string, string>> Properties { get; set; } = new List<KeyValuePair<string, string>>();

            /// <summary>
            /// Gets/sets the public url used to access the uploaded media.
            /// </summary>
            public string PublicUrl { get; set; }

            /// <summary>
            /// Gets/sets the file size.
            /// </summary>
            public string Size { get; set; }


            public long? Order { get; set; }

            /// <summary>
            /// Gets/sets the optional width.
            /// </summary>
            public int? Width { get; set; }

            /// <summary>
            /// Gets/sets the optional height.
            /// </summary>
            public int? Height { get; set; }

            /// <summary>
            /// An optional version url for a different size when requested via the list api call on the MediaApiController. Only use when expecting this call.
            /// </summary>
            public string AltVersionUrl { get; set; }

            /// <summary>
            /// Gets/sets the last modification date.
            /// </summary>
            public DateTimeOffset? ModificationDate { get; set; }

            public long CultureId { get; set; }

            public long SiteId { get; set; }

            public EStatus Status { get; set; }

            public string AltUrl { get; set; }

        }

        /// <summary>
        /// Gets/sets the available folders.
        /// </summary>
        public IList<FolderItem> Folders { get; set; } = new List<FolderItem>();

        /// <summary>
        /// Gets/sets the available media items.
        /// </summary>
        public IList<MediaItem> Media { get; set; } = new List<MediaItem>();

        /// <summary>
        /// Gets/sets the optional folder id.
        /// </summary>
        public long? CurrentFolderId { get; set; }

        /// <summary>
        /// Gets/sets the optinal folder name
        /// </summary>
        public string CurrentFolderName { get; set; }

        /// <summary>
        /// Gets/sets the optional parent id.
        /// </summary>
        public long? ParentFolderId { get; set; }

        /// <summary>
        /// Gets/sets if the current folder can be deleted.
        /// </summary>
        public bool CanDelete { get; set; }

        /// <summary>
        /// Gets/sets the optional status message from the last operation.
        /// </summary>
        public StatusMessage Status { get; set; }

        /// <summary>
        /// Gets/sets the recommended view mode for the folder.
        /// </summary>
        public string ViewMode { get; set; }

        /// <summary>
        /// Gets/sets the media folder structure.
        /// </summary>
        public Core.Models.MediaStructure  Structure { get; set; }

        /// <summary>
        /// Gets/sets the media folder structure.
        /// </summary>
        public List<MediaFolderSimple> CurrentFolderBreadcrumb { get; set; }

        /// <summary>
        /// Gets/sets the amount of media files at root level.
        /// </summary>
        public int RootCount { get; set; }

        /// <summary>
        /// Gets/sets the total amount of media files.
        /// </summary>
        public int TotalCount { get; set; }
       
            public int TotalCountsFolder{ get; set; }

      

            public int TotalCountsMedia { get; set; }

    }



    //public class MediaRequestVM
    //{


    //    public long? folderId { get; set; } = null;

    //    public MediaType? filter { get; set; } = null;
    //    public int? width { get; set; } = null;
    //    public int? height  { get; set; } = null;

    //    public pagination folder { get; set; } = new pagination();
    //    public pagination media { get; set; } = new pagination();


    //}
    //public class pagination
    //{
    //    public int? PageSize { get; set; } = null;


    //    public int? PageNumber { get; set; } = null;

    //}


}