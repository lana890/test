
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using EskaCMS.Infrastructure.Models;
using Newtonsoft.Json;

namespace EskaCMS.Entities.Entity.Core
{
    [Serializable]
    public class MediaDetails : EntityBase
    {


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

      
        public long CreatedById { get; set; }
        public User CreatedBy { get; set; }

        public long? ModifiedById { get; set; }
        public User ModifiedBy { get; set; }

        public long MediaId { get; set; }
        public Media Media { get; set; }
        public long CultureId { get; set; }
        public virtual SitesCultures Culture { get; set; }

    }
}