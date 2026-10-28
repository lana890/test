using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Reviews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Reviews.Models.ReplayModels
{
    public class ReplyViewModel
    {
        public long Id { get; set; } 
        public string Comment { get; set; }
        public string ReplierName { get; set; }
        public string EntityName { get; set; }
        public long UserId { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public ReviewStatus Status { get; set; }
        public long ReviewId { get; set; }
        public IList<ReplyViewMedia> Medias { get; set; } = new List<ReplyViewMedia>();

        public Review Review { get; set; }

    }
    public class ReplyViewMedia
    {
        public long Id { get; set; }
        public string URL { get; set; }
        public string ContentType { get; set; }
    }

}
