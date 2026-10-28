using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Entities.Enum;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
namespace EskaCMS.Entities.Entity.UsersAndRolesManagement
{
    public class Analytics_TrafficSource : EntityBase
    {
        public int Id { get; set; }
        public long CustomerId { get; set; }
        public User User { get; set; }
        public long CartId { get; set; }
        public Cart Cart { get; set; }
        public TrafficSource TrafficSource { get; set; }
        public EPlatformSource Channel { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long SitId { get; set; }
        public Site Site { get; set; }
    }
}