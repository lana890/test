
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;


namespace EskaCMS.Entities.Entity.Core
{
    public class UsersSites : EntityBase
    {
        public UsersSites()
        {
            CommentPermissions = true;
            UserConnectionStatus = ConnectionStatus.None;

        }


        public bool IsDefault { get; set; }

        public long SiteId { get; set; }
        public Site Site { get; set; }

        public long? CultureId { get; set; }
        public SitesCultures Culture { get; set; }

        public long? CurrencyId { get; set; }
        public CurrencyRates Currency { get; set; }

        public long UserId { get; set; }
        public User User { get; set; }

        public string CountryId { get; set; }

        public EskaCMS.Entities.Entity.Addresses.Country Country { get; set; }

        public bool IsLockout { get; set; }

        public bool CommentPermissions { get; set; }
        public DateTimeOffset? LastSeen { get; set; }
   

        public ConnectionStatus UserConnectionStatus { get; set; }
        public long? WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; }

    }
   
}
