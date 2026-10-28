using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Entities.Entity.Reviews
{

        public class ReplyMedia : EntityBase
        {
            public long MediaId { get; set; }
            public Media Media { get; set; }

            public long ReplyId { get; set; }
            public Reply Reply { get; set; }
        }
    }


