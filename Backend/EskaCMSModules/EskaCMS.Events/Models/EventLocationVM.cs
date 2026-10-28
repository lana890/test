using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.EventEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Events.Models
{
  public  class EventLocationVM
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string LocationMapURL { get; set; }

        public long SiteId { get; set; }

        public long CultureId { get; set; }
        public long? CategoryId { get; set; }

        public EStatus Status { get; set; }
        public long CreatedById { get; set; }
        

        public long? ThumbnailId { get; set; }
        public DateTimeOffset CreationDate { get; set; }
      
        public DateTimeOffset? ModificationDate { get; set; }
        public long? ModifiedById { get; set; }

        public string Settings { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public EventPlace EventPlace { get; set; }

        public List<EventLocationDetailsVM> EventLocationDetails { get; set; }

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


    public class EventLocationDetailsVM
    {
      
        public string Name { get; set; }

        public long CultureId { get; set; }
        public string CultureCode { get; set; }



    }


  

     


    }
