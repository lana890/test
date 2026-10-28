
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Enum;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Media.Models
{
    [Serializable]
    public sealed class MediaViewModel : MediaBaseViewModel
    {
        /// <summary>
        /// Gets/sets the user defined properties.
        /// </summary>
        public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets/sets the available versions.
        /// </summary>
        public IList<MediaVersionViewModel> Versions { get; set; } = new List<MediaVersionViewModel>();
    }

    [Serializable]
    public abstract class MediaBaseViewModel
    {
        /// <summary>
        /// Gets/sets the unique id.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets/sets the optional folder id.
        /// </summary>
        public Guid? FolderId { get; set; }

        /// <summary>
        /// Gets/sets the media type.
        /// </summary>
        public MediaType Type { get; set; }

        /// <summary>
        /// Gets/sets the filename.
        /// </summary>
        [Required]
        [StringLength(128)]
        public string Filename { get; set; }

        /// <summary>
        /// Gets/sets the content type.
        /// </summary>
        [Required]
        [StringLength(256)]
        public string ContentType { get; set; }

        /// <summary>
        /// Gets/sets the optional title.
        /// </summary>
        [StringLength(128)]
        public string Title { get; set; }

        /// <summary>
        /// Gets/sets the optional alt text.
        /// </summary>
        [StringLength(128)]
        public string AltText { get; set; }

        /// <summary>
        /// Gets/sets the optional description.
        /// </summary>
        [StringLength(512)]
        public string Description { get; set; }

        /// <summary>
        /// Gets/sets the file size in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets/sets the public url.
        /// </summary>
        public string PublicUrl { get; set; }

        /// <summary>
        /// Gets/sets the optional width. This only applies
        /// if the media asset is an image.
        /// </summary>
        public int? Width { get; set; }

        /// <summary>
        /// Gets/sets the optional height. This only applies
        /// if the media asset is an image.
        /// </summary>
        public int? Height { get; set; }

        /// <summary>
        /// Gets/sets the created date.
        /// </summary>
        public DateTimeOffset Created { get; set; }

        /// <summary>
        /// Gets/sets the last modification date.
        /// </summary>
        public DateTime LastModified { get; set; }

        internal static IDictionary<string, string> DeSerializeProperties(string str)
        {
            var properties = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(str))
            {
                foreach (var prop in JsonConvert.DeserializeObject<IEnumerable<Tuple<string, string>>>(str))
                {
                    if (!string.IsNullOrEmpty(prop.Item1))
                    {
                        properties[prop.Item1] = prop.Item2;
                    }
                }
            }
            return properties;
        }

        internal static string SerializeProperties(IDictionary<string, string> properties)
        {
            var items = new List<Tuple<string, string>>();

            foreach (var key in properties.Keys)
            {
                items.Add(new Tuple<string, string>(key, properties[key]));
            }
            return JsonConvert.SerializeObject(items);
        }
    }
}