
using EskaCMS.Infrastructure.Models;
using System;
using EskaCMS.Entities.Entity.Core;


namespace EskaCMS.Entities.Entity.Addresses
{
    public class UserAddress : EntityBase
    {
        public long UserId { get; set; }

        public User User { get; set; }

        public long AddressId { get; set; }

        public Address Address { get; set; }

        public EskaCMS.Entities.Enum.AddressType AddressType { get; set; }

        public DateTimeOffset? LastUsedOn { get; set; }
    }
}
