using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;

namespace EskaCMS.Entities.Entity.Core
{
    public class UserFiles : EntityBase
    {
        public UserFiles()
        {
            CreationDate = DateTimeOffset.Now;
            ModificationDate = DateTimeOffset.Now;
        }

        public User User { get; set; }
        public long UserId { get; set; }

        public Media Media { get; set; }
        public long MediaId { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public long? ModifiedById { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
    }
}
