
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Entities.Entity.Core
{

    public  class Options_in_Role : EntityBase
    {

       
        public long  CreatedById { get; set; }
        public long? ModifiedById { get; set; }
        public User CreatedBy { get; set; }
        public DateTimeOffset    CreationDate { get; set; }
        public User ModifiedBy { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public Role Role { get; set; }
        public long RoleId { get; set; }
        public Option Option { get; set; }
        public long OptionId { get; set; }
        public short OptionValue { get; set; }


    }
}
