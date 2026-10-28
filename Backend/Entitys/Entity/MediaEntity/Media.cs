
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using EskaCMS.Entities.Entity.MediaEntity;
using EskaCMS.Entities.Entity.Reviews;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Models;
using Newtonsoft.Json;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Core
{
    [Serializable]
    public class Media : EntityBase
    {
        public Media()
        {
            NewsLetterMedias=new HashSet<NewsLetterMedias>();
            ReviewMedias = new HashSet<ReviewMedia>();
           ReplyMedia = new HashSet<ReplyMedia>();

        }

        // public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets/sets the available versions.
        /// </summary>
        public IList<MediaVersion> Versions { get; set; } = new List<MediaVersion>();

        /// <summary>
        /// Gets/sets the unique id.
        /// </summary>
     //   public Guid Id { get; set; }

        /// <summary>
        /// Gets/sets the optional folder id.
        /// </summary>
        public long? FolderId { get; set; }

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
      
        /// <summary>
        /// Gets/sets the file size in bytes.
        /// </summary>
        public long Size { get; set; }


        /// <summary>
        /// Gets/sets the file Order.
        /// </summary>
        public long? Order { get; set; }

        /// <summary>
        /// Gets/sets the public url.
        /// </summary>
        public string PublicUrl { get; set; }

        public string AltUrl { get; set; }

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
        public DateTimeOffset CreationDate { get; set; }

        /// <summary>
        /// Gets/sets the last modification date.
        /// </summary>
        public DateTimeOffset? ModificationDate { get; set; }

        public  Site Site { get; set; }

        public long SiteId { get; set; }

        public EStatus Status { get; set; }
        public ICollection<UserFiles> UserFiles { get; set; }
        public IList<MediaDetails> MediaDetails { get; set; }
        public ICollection<NewsLetterMedias> NewsLetterMedias { get; set; }
        public ICollection<ReviewMedia> ReviewMedias { get; set; }
        public ICollection<ReplyMedia> ReplyMedia { get; set; }

        public ICollection<MediaRole> MediaRoles { get; set; }

        public long CreatedById { get; set; }
        public long? ModifiedById { get; set; }

        /// <summary>
        /// Gets/sets the user defined properties serialized as JSON.
        /// </summary>
        public string Properties { get; set; }

        /// <summary>
        /// Gets/sets the optional folder.
        /// </summary>
        public MediaFolders Folder { get; set; }
        //public MediaFoldersDetails FolderDetails { get; set; }

        /// <summary>
        /// Gets/sets the available versions.
        /// </summary>
        //public IList<MediaVersion> Versions { get; set; } = new List<MediaVersion>();

        public static implicit operator MediaVM(Media m)
        {
            return m == null ? null : new MediaVM
            {
                Id = m.Id,
                FolderId = m.FolderId,
                Type = m.Type,
                Filename = m.Filename,
                ContentType = m.ContentType,
                Properties = DeSerializeProperties(m.Properties),
                Size = m.Size,
                Order = m.Order,
                Width = m.Width,
                Height = m.Height,
                CreatedById = m.CreatedById,
                SiteId = m.SiteId,
                PublicUrl = m.PublicUrl,
                MediaDetails = m.MediaDetails,
                ModificationDate = m.ModificationDate,
                Status=m.Status,
                AltUrl=m.AltUrl,
                Versions = m.Versions.Select(v => new MediaVersion
                {
                    Id = v.Id,
                    Size = v.Size,
                    Width = v.Width,
                    Height = v.Height,
                    FileExtension = v.FileExtension
                }).ToList()
            };
        }

        public static implicit operator Media(MediaVM m)
        {
            return new Media
            {
                FolderId = m.FolderId,
                Type = m.Type,
                Filename = m.Filename,
                ContentType = m.ContentType,
                Properties = SerializeProperties(m.Properties),
                Size = m.Size,
                Order = m.Order,
                Width = m.Width,
                Height = m.Height,
                CreatedById = m.CreatedById,
                ModificationDate = m.ModificationDate,
                SiteId = m.SiteId,
                PublicUrl = m.PublicUrl,
                AltUrl = m.AltUrl,
                Status=m.Status,
                MediaDetails = (IList<MediaDetails>)m.MediaDetails,
                Versions = m.Versions.Select(v => new MediaVersion
                {
                    Id = v.Id,
                    Size = v.Size,
                    Width = v.Width,
                    Height = v.Height,
                    FileExtension = v.FileExtension
                }).ToList()
            };
        }
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

        public static string SerializeProperties(IDictionary<string, string> properties)
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