using EskaCMS.Infrastructure.Models;
using System;
using EskaCMS.Entities.Entity.Core;
using static EskaCMS.Entities.Enum.EventEnums;

namespace EskaCMS.Entities.Entity.Events
{
  public  class EventLocation : EntityBase
    {
      
        public string LocationMapURL { get; set; }
        public virtual EskaCMS.Entities.Entity.Core.Site Site { get; set; }
        public long SiteId { get; set; }
        public EskaCMS.Entities.Enum.GeneralEnums.EStatus Status { get; set; }
        public long CreatedById { get; set; }
        public virtual User CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public virtual User ModifiedBy { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public long? ModifiedById { get; set; }

        public long? CategoryId { get; set; }
        public virtual Category Category { get; set; }
        public string Settings { get; set; }

        public virtual Media Thumbnail { get; set; }

        public long? ThumbnailId { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public EventPlace EventPlace { get; set; }
        public List<EventLocationDetails> EventLocationDetails { get; set; }


        /*
         * Settings
        // ,[EVENT_SETTINGS_NAME]
      ,[EVENT_SETTINGS_USE_RECURRENCE]
      ,[EVENT_SETTINGS_USE_CATEGORY]
      ,[EVENT_SETTINGS_USE_NOTIFICATION]
      ,[EVENT_SETTINGS_USE_REMINDER]
      ,[EVENT_SETTINGS_USE_RSS]
      ,[EVENT_SETTINGS_USE_IMAGE]
      ,[EVENT_SETTINGS_PORTAL_ID]
      ,[EVENT_SETTINGS_LANGUAGE_ID]
      ,[EVENT_SETTINGS_USE_REGISTRATION] */


    }
}
