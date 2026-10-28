

using EskaCMS.Entities.Enum;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Runtime
{
    public static class MediaManager
    {
        private static readonly object InitLock = new object();
        private static bool IsInitialized;


 
        /// <summary>
        /// A registered media type.
        /// </summary>
        public class MediaTypeItem
        {
            /// <summary>
            /// Gets/sets the file extension.
            /// </summary>
            public string Extension { get; set; }

            /// <summary>
            /// Gets/sets the content type.
            /// </summary>
            public string ContentType { get; set; }

            /// <summary>
            /// If image processing should be applied to this media type.
            /// </summary>
            public bool AllowProcessing { get; set; }
        }

        /// <summary>
        /// A list of media types.
        /// </summary>
        public class MediaTypeList : List<MediaTypeItem>
        {
            private readonly bool _allowProcessing;

            /// <summary>
            /// Default constructor
            /// </summary>
            /// <param name="allowProcessing">If image processing should be applied by default</param>
            public MediaTypeList(bool allowProcessing = false)
            {
                _allowProcessing = allowProcessing;
            }

            /// <summary>
            /// Adds a new media type.
            /// </summary>
            /// <param name="extension">The file extension</param>
            /// <param name="contentType">The content type</param>
            /// <param name="allowProcessing">If image processing should be allowed</param>
            public void Add(string extension, string contentType, bool? allowProcessing = null)
            {
                Add(new MediaTypeItem
                {
                    Extension = extension.ToLower(),
                    ContentType = contentType,
                    AllowProcessing = allowProcessing.HasValue ? allowProcessing.Value : _allowProcessing
                });
            }

            /// <summary>
            /// Gets if the list contains an item with the given extension.
            /// </summary>
            /// <param name="extension">The file extension</param>
            /// <returns>If the extension exists</returns>
            public bool ContainsExtension(string extension)
            {
                var x = this;
                return this.Any(t => t.Extension != null && t.Extension.Equals(extension, System.StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Gets/sets the currently accepted document extensions.
        /// </summary>
        public static MediaTypeList Documents { get; set; } = new MediaTypeList();

        /// <summary>
        /// Gets/sets the currently accepted image extensions.
        /// </summary>
        public static MediaTypeList Images { get; set; } = new MediaTypeList(true);

        /// <summary>
        /// Gets/sets the currently accepted video extensions.
        /// </summary>
        public static MediaTypeList Videos { get; set; } = new MediaTypeList();

        /// <summary>
        /// Gets/sets the currently accepted audio extensions.
        /// </summary>
        public static MediaTypeList Audio { get; set; } = new MediaTypeList();

        /// <summary>
        /// Gets/sets the currently accepted resource extensions.
        /// </summary>
        public static MediaTypeList Resources { get; set; } = new MediaTypeList();

        /// <summary>
        /// Gets/sets the currently registered meta properties for media.
        /// </summary>
        public static IList<string> MetaProperties { get; set; } = new List<string>();

        /// <summary>
        /// Checks if the given filename has a supported extension.
        /// </summary>
        /// <param name="filename">The path or filename</param>
        /// <returns>If it is supported</returns>
        public static bool IsSupported(string filename)
        {

            var extension = Path.GetExtension(filename);

            return Documents.ContainsExtension(extension) ||
                Images.ContainsExtension(extension) ||
                Videos.ContainsExtension(extension) ||
                Audio.ContainsExtension(extension) ||
                Resources.ContainsExtension(extension);
        }

        
        
        public static void Init()
        {
            if (IsInitialized)
            {
                return;
            }

            lock (InitLock)
            {
                if (IsInitialized)
                {
                    return;
                }

                Images = new MediaTypeList();
                Documents = new MediaTypeList();
                Videos = new MediaTypeList();
                Audio = new MediaTypeList();
                Resources = new MediaTypeList();

                Documents.Add(".pdf", "application/pdf");
                Documents.Add(".doc", "application/msword");
                Documents.Add(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
                Documents.Add(".xls", "application/vnd.ms-excel");
                Documents.Add(".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                Documents.Add(".pptx", "PowerPointPresentation");
                Documents.Add(".pptm", "PowerPointMacro-EnabledPresentation");
                Documents.Add(".ppt", "PowerPoint97-2003Presentation");
                Documents.Add(".css", "text/css");
                Documents.Add(".js", "text/javascript");
                Images.Add(".avif", "image/avif");
                Images.Add(".jpg", "image/jpeg");
                Images.Add(".jpeg", "image/jpeg");
                Images.Add(".jfif", "image/jpeg");
                Images.Add(".gif", "image/gif");
                Images.Add(".png", "image/png");
                //Images.Add(".svg", "image/svg");
                Images.Add(".svg", "image/svg+xml");
                Images.Add(".ico", "image/x-icon");
                Images.Add(".webp", "image/webp");
                Videos.Add(".mp4", "video/mp4");
                Audio.Add(".mp3", "audio/mpeg");
                Audio.Add(".wav", "audio/wav");
                Resources.Add(".OTF", "font");
                Resources.Add(".ttf", "font");
                Resources.Add(".woff", "font");
                Resources.Add(".woff2", "font");

                IsInitialized = true;
            }

            
        }

        /// <summary>
        /// Gets the media type for the given filename based on
        /// its extension.
        /// </summary>
        /// <param name="filename">The path or filename</param>
        /// <returns>The media type</returns>
        public static MediaType GetMediaType(string filename)
        {
            var extension = Path.GetExtension(filename);

            if (Documents.ContainsExtension(extension))
            {
                return MediaType.Document;
            }

            if (Images.ContainsExtension(extension))
            {
                return MediaType.Image;
            }
            if (Videos.ContainsExtension(extension))
            {
                return MediaType.Video;
            }
            if (Audio.ContainsExtension(extension))
            {
                return MediaType.Audio;
            }
            if (Resources.ContainsExtension(extension))
            {
                return MediaType.Resource;
            }

            return MediaType.Unknown;
        }

        /// <summary>
        /// Gets the content type for the given filename based on
        /// its extension.
        /// </summary>
        /// <param name="filename">The path or filename</param>
        /// <returns>The media type</returns>
        public static string GetContentType(string filename)
        {



            var extension = Path.GetExtension(filename).ToLower();
            MediaTypeItem item = null;

            if ((item = Documents.SingleOrDefault(t => t.Extension == extension)) != null)
            {
                return item.ContentType;
            }

            if ((item = Images.SingleOrDefault(t => t.Extension == extension)) != null)
            {
                return item.ContentType;
            }
            if ((item = Videos.SingleOrDefault(t => t.Extension == extension)) != null)
            {
                return item.ContentType;
            }
            if ((item = Audio.SingleOrDefault(t => t.Extension == extension)) != null)
            {
                return item.ContentType;
            }
            if ((item = Resources.SingleOrDefault(t => t.Extension == extension)) != null)
            {
                return item.ContentType;
            }

            return "application/octet-stream";
        }

        /// <summary>
        /// Gets the media type item for the given filename based on its extensions.
        /// </summary>
        /// <param name="filename">The path or filename</param>
        /// <returns>The media type item</returns>
        public static MediaTypeItem GetItem(string filename)
        {
            // Setup media types
            //Documents.Add(".pdf", "application/pdf");
            //Images.Add(".jpg", "image/jpeg");
            //Images.Add(".jpeg", "image/jpeg");
            //Images.Add(".jfif", "image/jpeg");
            //Images.Add(".gif", "image/gif");
            //Images.Add(".png", "image/png");
            //Videos.Add(".mp4", "video/mp4");
            //Audio.Add(".mp3", "audio/mpeg");
            //Audio.Add(".wav", "audio/wav");
            var extension = Path.GetExtension(filename).ToLower();
            MediaTypeItem item = null;




            if ((item = Documents.FirstOrDefault(t => t.Extension == extension)) != null)
            {
                return item;
            }

            if ((item = Images.FirstOrDefault(t => t.Extension == extension)) != null)
            {
                return item;
            }
            if ((item = Videos.FirstOrDefault(t => t.Extension == extension)) != null)
            {
                return item;
            }
            if ((item = Audio.FirstOrDefault(t => t.Extension == extension)) != null)
            {
                return item;
            }
            if ((item = Resources.FirstOrDefault(t => t.Extension == extension)) != null)
            {
                return item;
            }

            return null;
        }
    }
}
